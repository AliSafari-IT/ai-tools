using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LogCopilot.Application.Interfaces;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.Clustering;
using LogCopilot.Infrastructure.Data;
using LogCopilot.Infrastructure.Parsers;
using LogCopilot.Infrastructure.Storage;
using LogCopilot.Infrastructure.Tracing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LogLevel = LogCopilot.Domain.Entities.LogLevel;

namespace LogCopilot.Infrastructure.Services;

public class IngestionService : IIngestionService
{
    private readonly LogCopilotDbContext _context;
    private readonly IFileStorage _fileStorage;
    private readonly ILogger<IngestionService> _logger;
    private readonly ClusteringService _clusteringService;
    private readonly TraceBuilder _traceBuilder;
    private readonly Dictionary<Guid, CancellationTokenSource> _cancellationTokens = new();

    public IngestionService(
        LogCopilotDbContext context,
        IFileStorage fileStorage,
        ILogger<IngestionService> logger,
        ClusteringService clusteringService,
        TraceBuilder traceBuilder)
    {
        _context = context;
        _fileStorage = fileStorage;
        _logger = logger;
        _clusteringService = clusteringService;
        _traceBuilder = traceBuilder;
    }

    public async Task ProcessIngestionJobAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _context.IngestionJobs
            .Include(j => j.UploadSession)
            .FirstOrDefaultAsync(j => j.Id == jobId);

        if (job == null)
        {
            _logger.LogWarning("Ingestion job {JobId} not found", jobId);
            return;
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cancellationTokens[jobId] = cts;

        try
        {
            _logger.LogInformation("Starting ingestion job {JobId} for organization {OrgId}", jobId, job.OrganizationId);
            job.Status = IngestionJobStatus.Running;
            job.StartedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var uploadedFile = await _context.UploadedFiles
                .FirstOrDefaultAsync(f => f.Id == job.UploadedFileId);

            if (uploadedFile == null)
            {
                throw new Exception($"Uploaded file {job.UploadedFileId} not found");
            }

            _logger.LogInformation("Processing file {FileName} with type {FileType}", uploadedFile.OriginalFileName, uploadedFile.FileType);
            var stream = await _fileStorage.GetFileStreamAsync(uploadedFile.StoragePath);
            var parser = GetParser(uploadedFile.FileType);
            
            _logger.LogInformation("Starting to parse file with parser type {ParserType}", parser.GetType().Name);
            var events = await parser.ParseAsync(stream);

            _logger.LogInformation("Parsed {EventCount} events from file", events.Count);
            if (events.Count == 0)
            {
                _logger.LogWarning("No events parsed from file {FileName}. File may be empty or in unsupported format.", uploadedFile.OriginalFileName);
            }
            job.TotalLines = events.Count;

            foreach (var parsedEvent in events)
            {
                cts.Token.ThrowIfCancellationRequested();

                try
                {
                    await ProcessParsedEventAsync(parsedEvent, job);
                    job.SuccessfulLines++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process log event");
                    job.FailedLines++;
                }
                job.ProcessedLines++;
            }

            _logger.LogInformation("Ingestion job {JobId} completed: {SuccessfulLines} successful, {FailedLines} failed", jobId, job.SuccessfulLines, job.FailedLines);
            job.Status = IngestionJobStatus.Completed;
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Ingestion job {JobId} was cancelled", jobId);
            job.Status = IngestionJobStatus.Cancelled;
            job.ErrorMessage = "Job was cancelled by user";
            job.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ingestion job {JobId} failed", jobId);
            job.Status = IngestionJobStatus.Failed;
            job.ErrorMessage = ex.Message;
            job.CompletedAt = DateTime.UtcNow;
        }
        finally
        {
            _cancellationTokens.Remove(jobId);
            cts.Dispose();
        }

        await _context.SaveChangesAsync();
    }

    public async Task CancelIngestionJobAsync(Guid jobId)
    {
        if (_cancellationTokens.TryGetValue(jobId, out var cts))
        {
            _logger.LogInformation("Cancelling ingestion job {JobId}", jobId);
            cts.Cancel();
        }
        else
        {
            _logger.LogWarning("Ingestion job {JobId} not found or already completed", jobId);
        }

        await Task.CompletedTask;
    }

    private async Task ProcessParsedEventAsync(Parsers.Models.ParsedLogEvent parsedEvent, IngestionJob job)
    {
        var eventHash = ComputeEventHash(parsedEvent);
        
        var existingEvent = await _context.LogEvents
            .FirstOrDefaultAsync(e => e.OrganizationId == job.OrganizationId && e.EventHash == eventHash);

        if (existingEvent != null)
            return;

        var logEvent = new LogEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = job.OrganizationId,
            UploadSessionId = job.UploadSessionId,
            Timestamp = parsedEvent.Timestamp,
            Level = ParseLogLevel(parsedEvent.Level),
            MessageTemplate = parsedEvent.MessageTemplate,
            RenderedMessage = parsedEvent.RenderedMessage,
            RawJson = parsedEvent.RawJson,
            PropertiesJson = parsedEvent.Properties != null ? SerializePropertiesJson(parsedEvent.Properties) : null,
            TraceId = parsedEvent.TraceId,
            SpanId = parsedEvent.SpanId,
            ParentSpanId = parsedEvent.ParentSpanId,
            RequestId = parsedEvent.RequestId,
            CorrelationId = parsedEvent.CorrelationId,
            EventHash = eventHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = job.CreatedBy,
            UpdatedBy = job.UpdatedBy
        };

        _context.LogEvents.Add(logEvent);
        await _context.SaveChangesAsync();

        if (parsedEvent.HttpDetails != null)
        {
            var httpDetails = new HttpEventDetails
            {
                Id = Guid.NewGuid(),
                LogEventId = logEvent.Id,
                Method = parsedEvent.HttpDetails.Method,
                Path = parsedEvent.HttpDetails.Path,
                QueryString = parsedEvent.HttpDetails.QueryString,
                StatusCode = parsedEvent.HttpDetails.StatusCode,
                DurationMs = parsedEvent.HttpDetails.DurationMs,
                ClientIp = parsedEvent.HttpDetails.ClientIp,
                UserAgent = parsedEvent.HttpDetails.UserAgent,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.HttpEventDetails.Add(httpDetails);
        }

        if (parsedEvent.ExceptionDetails != null)
        {
            var signature = ClusteringService.ComputeExceptionSignature(
                parsedEvent.ExceptionDetails.Type,
                parsedEvent.ExceptionDetails.StackTrace);

            var exceptionDetails = new ExceptionDetails
            {
                Id = Guid.NewGuid(),
                LogEventId = logEvent.Id,
                Type = parsedEvent.ExceptionDetails.Type,
                Message = parsedEvent.ExceptionDetails.Message,
                StackTrace = parsedEvent.ExceptionDetails.StackTrace,
                SignatureHash = signature,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ExceptionDetails.Add(exceptionDetails);
        }

        await _context.SaveChangesAsync();

        await _traceBuilder.ProcessEventForTracingAsync(logEvent);
        await _clusteringService.ProcessEventForClusteringAsync(logEvent);
    }

    private ILogParser GetParser(LogFileType fileType)
    {
        return fileType switch
        {
            LogFileType.SerilogJsonLines => new SerilogJsonParser(),
            LogFileType.SerilogPlainText => new SerilogPlainTextParser(),
            LogFileType.NginxAccessLog => new NginxAccessLogParser(),
            LogFileType.NginxErrorLog => new NginxErrorLogParser(),
            _ => throw new NotSupportedException($"File type {fileType} is not supported")
        };
    }

    private string ComputeEventHash(Parsers.Models.ParsedLogEvent parsedEvent)
    {
        var hashInput = $"{parsedEvent.Timestamp:O}:{parsedEvent.Level}:{parsedEvent.RenderedMessage}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
        return Convert.ToHexString(bytes).ToLower();
    }

    private LogLevel ParseLogLevel(string level)
    {
        return level.ToLower() switch
        {
            "verbose" => LogLevel.Verbose,
            "debug" => LogLevel.Debug,
            "information" => LogLevel.Information,
            "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "fatal" => LogLevel.Fatal,
            _ => LogLevel.Information
        };
    }

    private string SerializePropertiesJson(Dictionary<string, object> properties)
    {
        try
        {
            var sanitized = new Dictionary<string, object?>();
            foreach (var kvp in properties)
            {
                var sanitizedValue = SanitizeValue(kvp.Value);
                sanitized[kvp.Key] = sanitizedValue;
            }
            return JsonSerializer.Serialize(sanitized);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to serialize properties, returning empty object");
            return "{}";
        }
    }

    private object? SanitizeValue(object? value)
    {
        if (value == null)
            return null;

        var type = value.GetType();
        
        if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal))
            return value;

        if (value is DateTime dt)
            return dt;

        if (value is System.Collections.IDictionary dict)
        {
            var result = new Dictionary<string, object?>();
            foreach (var key in dict.Keys)
            {
                if (key != null)
                {
                    var dictValue = dict[key];
                    result[key.ToString() ?? ""] = SanitizeValue(dictValue);
                }
            }
            return result;
        }

        if (value is System.Collections.IEnumerable enumerable && !(value is string))
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(SanitizeValue(item));
            }
            return list;
        }

        return value.ToString() ?? "";
    }
}
