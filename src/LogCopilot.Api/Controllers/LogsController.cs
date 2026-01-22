using System.Security.Claims;
using LogCopilot.Application.Interfaces;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Data;
using LogCopilot.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Api.Controllers;

[ApiController]
[Route("api/logs")]
[Authorize]
public class LogsController : ControllerBase
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<LogsController> _logger;
    private readonly LogCopilotDbContext _context;

    public LogsController(
        IBackgroundTaskQueue taskQueue,
        IFileStorage fileStorage,
        ILogger<LogsController> logger,
        LogCopilotDbContext context)
    {
        _taskQueue = taskQueue;
        _fileStorage = fileStorage;
        _logger = logger;
        _context = context;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFileCollection files)
    {
        _logger.LogInformation("Upload endpoint called with {FileCount} files", files?.Count ?? 0);
        
        if (files == null || files.Count == 0)
            return BadRequest(new { message = "No files provided" });

        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "User ID not found in token" });

            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return Unauthorized(new { message = "Organization ID not found in token" });
            
            _logger.LogInformation("Upload authorized for user {UserId} in org {OrgId}", userId, organizationId);

            var userGuid = Guid.Parse(userId);
            var orgGuid = Guid.Parse(organizationId);

            var uploadSession = new UploadSession
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgGuid,
                Name = $"Upload {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                Status = UploadSessionStatus.Uploading,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userGuid,
                UpdatedBy = userGuid
            };

            var uploadedFileIds = new List<Guid>();
            var ingestionJobIds = new List<Guid>();

            foreach (var file in files)
            {
                if (file.Length == 0)
                    continue;

                var fileType = DetermineFileType(file.FileName);
                if (fileType == null)
                {
                    _logger.LogWarning("Unsupported file type: {FileName}", file.FileName);
                    continue;
                }

                var fileId = Guid.NewGuid();
                var fileName = $"{organizationId}/{uploadSession.Id}/{fileId}/{file.FileName}";

                string actualStoragePath;
                using (var stream = file.OpenReadStream())
                {
                    actualStoragePath = await _fileStorage.SaveFileAsync(stream, fileName);
                }

                var uploadedFile = new UploadedFile
                {
                    Id = fileId,
                    OrganizationId = orgGuid,
                    UploadSessionId = uploadSession.Id,
                    OriginalFileName = file.FileName,
                    StoragePath = actualStoragePath,
                    SizeBytes = file.Length,
                    FileType = fileType.Value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userGuid,
                    UpdatedBy = userGuid
                };

                uploadSession.Files.Add(uploadedFile);

                var ingestionJob = new IngestionJob
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgGuid,
                    UploadSessionId = uploadSession.Id,
                    UploadedFileId = fileId,
                    Status = IngestionJobStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    CreatedBy = userGuid,
                    UpdatedBy = userGuid
                };

                uploadSession.IngestionJobs.Add(ingestionJob);
                uploadedFileIds.Add(fileId);
                ingestionJobIds.Add(ingestionJob.Id);

                _logger.LogInformation("File uploaded: {FileName} ({FileId})", file.FileName, fileId);
            }

            if (uploadedFileIds.Count == 0)
                return BadRequest(new { message = "No valid files were uploaded" });

            uploadSession.Status = UploadSessionStatus.Ready;
            await _context.UploadSessions.AddAsync(uploadSession);
            await _context.SaveChangesAsync();

            foreach (var jobId in ingestionJobIds)
            {
                var capturedJobId = jobId;
                var capturedOrgId = orgGuid;
                
                _logger.LogInformation("Enqueued ingestion job {JobId} for upload session {UploadSessionId}", capturedJobId, uploadSession.Id);

                await _taskQueue.EnqueueAsync(async (serviceProvider, cancellationToken) =>
                {
                    var logger = serviceProvider.GetRequiredService<ILogger<LogsController>>();
                    var context = serviceProvider.GetRequiredService<LogCopilotDbContext>();
                    var ingestionService = serviceProvider.GetRequiredService<IIngestionService>();

                    try
                    {
                        var job = await context.IngestionJobs
                            .FirstOrDefaultAsync(j => j.Id == capturedJobId && j.OrganizationId == capturedOrgId, cancellationToken);

                        if (job == null)
                        {
                            logger.LogWarning("Ingestion job {JobId} not found", capturedJobId);
                            return;
                        }

                        if (job.Status != IngestionJobStatus.Pending)
                        {
                            logger.LogInformation("Ingestion job {JobId} already processed with status {Status}", capturedJobId, job.Status);
                            return;
                        }

                        logger.LogInformation("Starting ingestion job {JobId}", capturedJobId);
                        await ingestionService.ProcessIngestionJobAsync(capturedJobId);
                        logger.LogInformation("Completed ingestion job {JobId}", capturedJobId);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process ingestion job {JobId}", capturedJobId);
                    }
                });
            }

            return Ok(new
            {
                message = "Files uploaded successfully",
                sessionId = uploadSession.Id,
                uploadedFileIds = uploadedFileIds,
                count = uploadedFileIds.Count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File upload failed");
            return StatusCode(500, new { message = "File upload failed", error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs([FromQuery] int skip = 0, [FromQuery] int take = 50, [FromQuery] string? search = null, [FromQuery] string? level = null)
    {
        var organizationId = User.FindFirst("OrganizationId")?.Value;
        if (string.IsNullOrEmpty(organizationId))
            return Unauthorized(new { message = "Organization ID not found in token" });

        var orgGuid = Guid.Parse(organizationId);

        var query = _context.LogEvents
            .Where(e => e.OrganizationId == orgGuid)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.RenderedMessage.Contains(search) || (e.LogSource != null && e.LogSource.Name.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(level) && level != "All Levels")
        {
            if (Enum.TryParse<Domain.Entities.LogLevel>(level, out var logLevel))
            {
                query = query.Where(e => e.Level == logLevel);
            }
        }

        var total = await query.CountAsync();
        var logs = await query
            .Include(e => e.LogSource)
            .OrderByDescending(e => e.Timestamp)
            .Skip(skip)
            .Take(take)
            .Select(e => new
            {
                id = e.Id,
                timestamp = e.Timestamp,
                level = e.Level.ToString(),
                message = e.RenderedMessage,
                traceId = e.TraceId,
                source = e.LogSource != null ? e.LogSource.Name : "Unknown"
            })
            .ToListAsync();

        return Ok(new
        {
            total,
            skip,
            take,
            logs
        });
    }

    [HttpPost("cancel/{jobId}")]
    public async Task<IActionResult> CancelIngestionJob(Guid jobId)
    {
        try
        {
            var organizationId = User.FindFirst("OrganizationId")?.Value;
            if (string.IsNullOrEmpty(organizationId))
                return Unauthorized(new { message = "Organization ID not found in token" });

            var orgGuid = Guid.Parse(organizationId);

            var job = await _context.IngestionJobs
                .FirstOrDefaultAsync(j => j.Id == jobId && j.OrganizationId == orgGuid);

            if (job == null)
                return NotFound(new { message = "Ingestion job not found" });

            if (job.Status != IngestionJobStatus.Pending && job.Status != IngestionJobStatus.Running)
                return BadRequest(new { message = $"Cannot cancel job with status {job.Status}" });

            var ingestionService = HttpContext.RequestServices.GetRequiredService<IIngestionService>();
            await ingestionService.CancelIngestionJobAsync(jobId);

            return Ok(new { message = "Cancellation requested for ingestion job", jobId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel ingestion job");
            return StatusCode(500, new { message = "Failed to cancel ingestion job", error = ex.Message });
        }
    }

    [HttpGet("status/{sessionId}")]
    public async Task<IActionResult> GetStatus(Guid sessionId)
    {
        var session = await _context.UploadSessions
            .Include(s => s.IngestionJobs)
            .FirstOrDefaultAsync(s => s.Id == sessionId);

        if (session == null)
            return NotFound(new { message = "Upload session not found" });

        var jobs = session.IngestionJobs.ToList();
        var totalJobs = jobs.Count;
        var completedJobs = jobs.Count(j => j.Status == IngestionJobStatus.Completed);
        var failedJobs = jobs.Count(j => j.Status == IngestionJobStatus.Failed);
        var runningJobs = jobs.Count(j => j.Status == IngestionJobStatus.Running);
        var totalEvents = jobs.Sum(j => j.SuccessfulLines);

        var isComplete = completedJobs + failedJobs == totalJobs;

        return Ok(new
        {
            sessionId = session.Id,
            status = session.Status.ToString(),
            isComplete,
            totalJobs,
            completedJobs,
            failedJobs,
            runningJobs,
            totalEvents,
            jobs = jobs.Select(j => new
            {
                id = j.Id,
                status = j.Status.ToString(),
                totalLines = j.TotalLines,
                processedLines = j.ProcessedLines,
                successfulLines = j.SuccessfulLines,
                failedLines = j.FailedLines,
                errorMessage = j.ErrorMessage
            })
        });
    }

    private LogFileType? DetermineFileType(string fileName)
    {
        var lowerFileName = fileName.ToLower();
        var extension = Path.GetExtension(lowerFileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(lowerFileName);

        if (extension == ".json" || lowerFileName.Contains("serilog"))
            return LogFileType.SerilogJsonLines;

        if (lowerFileName.Contains("access"))
            return LogFileType.NginxAccessLog;

        if (lowerFileName.Contains("error"))
            return LogFileType.NginxErrorLog;

        if (extension == ".log")
            return LogFileType.SerilogPlainText;

        if (extension == ".txt" && (nameWithoutExt.Contains("log") || nameWithoutExt.Contains("serilog")))
            return LogFileType.SerilogPlainText;

        return null;
    }
}
