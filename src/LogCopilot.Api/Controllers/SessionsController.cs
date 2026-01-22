using LogCopilot.Api.Extensions;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly LogCopilotDbContext _context;
    private readonly ILogger<SessionsController> _logger;

    public SessionsController(LogCopilotDbContext context, ILogger<SessionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetSessions()
    {
        try
        {
            var organizationId = User.GetOrganizationId();

            var sessions = await _context
                .UploadSessions.Where(s => s.OrganizationId == organizationId)
                .Select(s => new SessionDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Status = s.Status.ToString(),
                    CreatedAt = s.CreatedAt,
                    FileCount = s.Files.Count,
                    LogEventCount = _context.LogEvents.Count(e => e.UploadSessionId == s.Id),
                    IngestionJobCount = s.IngestionJobs.Count,
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sessions");
            return StatusCode(
                500,
                new { message = "Error retrieving sessions", error = ex.Message }
            );
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSessionById(Guid id)
    {
        try
        {
            var organizationId = User.GetOrganizationId();

            var session = await _context
                .UploadSessions.Include(s => s.Files)
                .Include(s => s.IngestionJobs)
                .Where(s => s.Id == id && s.OrganizationId == organizationId)
                .FirstOrDefaultAsync();

            if (session == null)
                return NotFound();

            var logEventCount = await _context
                .LogEvents.Where(e => e.UploadSessionId == id)
                .CountAsync();

            var dto = new SessionDetailDto
            {
                Id = session.Id,
                Name = session.Name,
                Status = session.Status.ToString(),
                CreatedAt = session.CreatedAt,
                FileCount = session.Files.Count,
                LogEventCount = logEventCount,
                IngestionJobCount = session.IngestionJobs.Count,
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session {SessionId}", id);
            return StatusCode(
                500,
                new { message = "Error retrieving session", error = ex.Message }
            );
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteSession(Guid id)
    {
        try
        {
            var organizationId = User.GetOrganizationId();

            var session = await _context
                .UploadSessions.Where(s => s.Id == id && s.OrganizationId == organizationId)
                .FirstOrDefaultAsync();

            if (session == null)
                return NotFound();

            var logEventIds = await _context
                .LogEvents.Where(e => e.UploadSessionId == id)
                .Select(e => e.Id)
                .ToListAsync();

            var httpDetails = await _context
                .HttpEventDetails.Where(h => logEventIds.Contains(h.LogEventId))
                .ToListAsync();
            _context.HttpEventDetails.RemoveRange(httpDetails);

            var exceptionDetails = await _context
                .ExceptionDetails.Where(e => logEventIds.Contains(e.LogEventId))
                .ToListAsync();
            _context.ExceptionDetails.RemoveRange(exceptionDetails);

            var traceEvents = await _context
                .TraceEvents.Where(te => logEventIds.Contains(te.LogEventId))
                .ToListAsync();
            _context.TraceEvents.RemoveRange(traceEvents);

            var clusterEvents = await _context
                .ClusterEvents.Where(ce => logEventIds.Contains(ce.LogEventId))
                .ToListAsync();
            _context.ClusterEvents.RemoveRange(clusterEvents);

            var affectedTraceIds = traceEvents.Select(te => te.RequestTraceId).Distinct().ToList();
            var affectedClusterIds = clusterEvents
                .Select(ce => ce.IssueClusterId)
                .Distinct()
                .ToList();

            var orphanedTraces = await _context
                .RequestTraces.Where(t =>
                    affectedTraceIds.Contains(t.Id)
                    && !_context.TraceEvents.Any(te => te.RequestTraceId == t.Id)
                )
                .ToListAsync();
            _context.RequestTraces.RemoveRange(orphanedTraces);

            var orphanedClusters = await _context
                .IssueClusters.Where(c =>
                    affectedClusterIds.Contains(c.Id)
                    && !_context.ClusterEvents.Any(ce => ce.IssueClusterId == c.Id)
                )
                .ToListAsync();
            _context.IssueClusters.RemoveRange(orphanedClusters);

            var logEvents = await _context
                .LogEvents.Where(e => e.UploadSessionId == id)
                .ToListAsync();
            _context.LogEvents.RemoveRange(logEvents);

            var uploadedFiles = await _context
                .UploadedFiles.Where(f => f.UploadSessionId == id)
                .ToListAsync();
            if (uploadedFiles.Any())
            {
                _context.UploadedFiles.RemoveRange(uploadedFiles);
            }

            var ingestionJobs = await _context
                .IngestionJobs.Where(j => j.UploadSessionId == id)
                .ToListAsync();
            _context.IngestionJobs.RemoveRange(ingestionJobs);

            _context.UploadSessions.Remove(session);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", id);
            return StatusCode(500, new { message = "Error deleting session", error = ex.Message });
        }
    }

}

public class SessionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int FileCount { get; set; }
    public int LogEventCount { get; set; }
    public int IngestionJobCount { get; set; }
}

public class SessionDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int FileCount { get; set; }
    public int LogEventCount { get; set; }
    public int IngestionJobCount { get; set; }
}
