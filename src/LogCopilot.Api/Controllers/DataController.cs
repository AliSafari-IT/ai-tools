using LogCopilot.Api.Extensions;
using LogCopilot.Application.DTOs;
using LogCopilot.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Api.Controllers;

[ApiController]
[Route("api/data")]
[Authorize(Roles = "Admin")]
public class DataController : ControllerBase
{
    private readonly LogCopilotDbContext _context;
    private readonly ILogger<DataController> _logger;

    public DataController(LogCopilotDbContext context, ILogger<DataController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("purge/preview")]
    public async Task<IActionResult> PreviewPurge([FromBody] PurgeRequest request)
    {
        try
        {
            var organizationId = User.GetOrganizationId();
            var preview = await CalculatePurgePreview(organizationId, request);
            return Ok(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error previewing purge");
            return StatusCode(500, new { message = "Error previewing purge", error = ex.Message });
        }
    }

    [HttpPost("purge/execute")]
    public async Task<IActionResult> ExecutePurge([FromBody] PurgeExecuteRequest request)
    {
        try
        {
            var organizationId = User.GetOrganizationId();

            if (request.ConfirmToken != "DELETE")
                return BadRequest(new { message = "Invalid confirmation token. Must be 'DELETE'." });

            var result = await ExecutePurgeOperation(organizationId, request.PurgeRequest);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing purge");
            return StatusCode(500, new { message = "Error executing purge", error = ex.Message });
        }
    }

    private async Task<PurgePreview> CalculatePurgePreview(Guid organizationId, PurgeRequest request)
    {
        var logEventIds = new List<Guid>();

        if (request.Scope == "all")
        {
            logEventIds = await _context.LogEvents
                .Where(e => e.OrganizationId == organizationId)
                .Select(e => e.Id)
                .ToListAsync();
        }
        else if (request.Scope == "timeRange" && request.StartUtc.HasValue && request.EndUtc.HasValue)
        {
            logEventIds = await _context.LogEvents
                .Where(e => e.OrganizationId == organizationId &&
                           e.Timestamp >= request.StartUtc.Value &&
                           e.Timestamp <= request.EndUtc.Value)
                .Select(e => e.Id)
                .ToListAsync();
        }
        else if (request.Scope == "uploadSession" && request.UploadSessionId.HasValue)
        {
            logEventIds = await _context.LogEvents
                .Where(e => e.OrganizationId == organizationId &&
                           e.UploadSessionId == request.UploadSessionId.Value)
                .Select(e => e.Id)
                .ToListAsync();
        }

        var httpDetailsCount = await _context.HttpEventDetails
            .Where(h => logEventIds.Contains(h.LogEventId))
            .CountAsync();

        var exceptionDetailsCount = await _context.ExceptionDetails
            .Where(e => logEventIds.Contains(e.LogEventId))
            .CountAsync();

        var traceEventIds = await _context.TraceEvents
            .Where(te => logEventIds.Contains(te.LogEventId))
            .Select(te => te.Id)
            .ToListAsync();

        var clusterEventIds = await _context.ClusterEvents
            .Where(ce => logEventIds.Contains(ce.LogEventId))
            .Select(ce => ce.Id)
            .ToListAsync();

        var affectedTraceIds = await _context.TraceEvents
            .Where(te => traceEventIds.Contains(te.Id))
            .Select(te => te.RequestTraceId)
            .Distinct()
            .ToListAsync();

        var affectedClusterIds = await _context.ClusterEvents
            .Where(ce => clusterEventIds.Contains(ce.Id))
            .Select(ce => ce.IssueClusterId)
            .Distinct()
            .ToListAsync();

        var orphanedTracesCount = await _context.RequestTraces
            .Where(t => affectedTraceIds.Contains(t.Id) &&
                       !_context.TraceEvents.Any(te => te.RequestTraceId == t.Id && !traceEventIds.Contains(te.Id)))
            .CountAsync();

        var orphanedClustersCount = await _context.IssueClusters
            .Where(c => affectedClusterIds.Contains(c.Id) &&
                       !_context.ClusterEvents.Any(ce => ce.IssueClusterId == c.Id && !clusterEventIds.Contains(ce.Id)))
            .CountAsync();

        var reportsAffected = await _context.IncidentReports
            .Where(r => r.OrganizationId == organizationId &&
                       (affectedClusterIds.Contains(r.ClusterId ?? Guid.Empty) ||
                        affectedTraceIds.Contains(r.TraceId ?? Guid.Empty)))
            .CountAsync();

        return new PurgePreview
        {
            LogEventsCount = logEventIds.Count,
            HttpDetailsCount = httpDetailsCount,
            ExceptionDetailsCount = exceptionDetailsCount,
            TraceEventsCount = traceEventIds.Count,
            RequestTracesAffectedCount = affectedTraceIds.Count,
            OrphanedTracesCount = orphanedTracesCount,
            ClusterEventsCount = clusterEventIds.Count,
            IssueClustersAffectedCount = affectedClusterIds.Count,
            OrphanedClustersCount = orphanedClustersCount,
            ReportsAffectedCount = reportsAffected
        };
    }

    private async Task<PurgeResult> ExecutePurgeOperation(Guid organizationId, PurgeRequest request)
    {
        var logEventIds = new List<Guid>();

        if (request.Scope == "all")
        {
            logEventIds = await _context.LogEvents
                .Where(e => e.OrganizationId == organizationId)
                .Select(e => e.Id)
                .ToListAsync();
        }
        else if (request.Scope == "timeRange" && request.StartUtc.HasValue && request.EndUtc.HasValue)
        {
            logEventIds = await _context.LogEvents
                .Where(e => e.OrganizationId == organizationId &&
                           e.Timestamp >= request.StartUtc.Value &&
                           e.Timestamp <= request.EndUtc.Value)
                .Select(e => e.Id)
                .ToListAsync();
        }
        else if (request.Scope == "uploadSession" && request.UploadSessionId.HasValue)
        {
            logEventIds = await _context.LogEvents
                .Where(e => e.OrganizationId == organizationId &&
                           e.UploadSessionId == request.UploadSessionId.Value)
                .Select(e => e.Id)
                .ToListAsync();
        }

        var httpDetails = await _context.HttpEventDetails
            .Where(h => logEventIds.Contains(h.LogEventId))
            .ToListAsync();
        _context.HttpEventDetails.RemoveRange(httpDetails);

        var exceptionDetails = await _context.ExceptionDetails
            .Where(e => logEventIds.Contains(e.LogEventId))
            .ToListAsync();
        _context.ExceptionDetails.RemoveRange(exceptionDetails);

        var traceEvents = await _context.TraceEvents
            .Where(te => logEventIds.Contains(te.LogEventId))
            .ToListAsync();
        _context.TraceEvents.RemoveRange(traceEvents);

        var clusterEvents = await _context.ClusterEvents
            .Where(ce => logEventIds.Contains(ce.LogEventId))
            .ToListAsync();
        _context.ClusterEvents.RemoveRange(clusterEvents);

        var affectedTraceIds = traceEvents.Select(te => te.RequestTraceId).Distinct().ToList();
        var affectedClusterIds = clusterEvents.Select(ce => ce.IssueClusterId).Distinct().ToList();

        var orphanedTraces = await _context.RequestTraces
            .Where(t => affectedTraceIds.Contains(t.Id) &&
                       !_context.TraceEvents.Any(te => te.RequestTraceId == t.Id))
            .ToListAsync();
        _context.RequestTraces.RemoveRange(orphanedTraces);

        var orphanedClusters = await _context.IssueClusters
            .Where(c => affectedClusterIds.Contains(c.Id) &&
                       !_context.ClusterEvents.Any(ce => ce.IssueClusterId == c.Id))
            .ToListAsync();
        _context.IssueClusters.RemoveRange(orphanedClusters);

        if (request.ForcePurgeAllClusters)
        {
            var allAffectedClusters = await _context.IssueClusters
                .Where(c => affectedClusterIds.Contains(c.Id))
                .ToListAsync();
            _context.IssueClusters.RemoveRange(allAffectedClusters);
        }

        var logEvents = await _context.LogEvents
            .Where(e => logEventIds.Contains(e.Id))
            .ToListAsync();
        _context.LogEvents.RemoveRange(logEvents);

        await _context.SaveChangesAsync();

        return new PurgeResult
        {
            Success = true,
            LogEventsDeleted = logEventIds.Count,
            HttpDetailsDeleted = httpDetails.Count,
            ExceptionDetailsDeleted = exceptionDetails.Count,
            TraceEventsDeleted = traceEvents.Count,
            OrphanedTracesDeleted = orphanedTraces.Count,
            ClusterEventsDeleted = clusterEvents.Count,
            OrphanedClustersDeleted = orphanedClusters.Count,
            Message = $"Successfully purged {logEventIds.Count} log events and related data."
        };
    }

}

public class PurgeRequest
{
    public string Scope { get; set; } = "all";
    public DateTime? StartUtc { get; set; }
    public DateTime? EndUtc { get; set; }
    public Guid? UploadSessionId { get; set; }
    public bool ForcePurgeAllClusters { get; set; }
}

public class PurgeExecuteRequest
{
    public PurgeRequest PurgeRequest { get; set; } = new();
    public string ConfirmToken { get; set; } = "";
}

public class PurgePreview
{
    public int LogEventsCount { get; set; }
    public int HttpDetailsCount { get; set; }
    public int ExceptionDetailsCount { get; set; }
    public int TraceEventsCount { get; set; }
    public int RequestTracesAffectedCount { get; set; }
    public int OrphanedTracesCount { get; set; }
    public int ClusterEventsCount { get; set; }
    public int IssueClustersAffectedCount { get; set; }
    public int OrphanedClustersCount { get; set; }
    public int ReportsAffectedCount { get; set; }
}

public class PurgeResult
{
    public bool Success { get; set; }
    public int LogEventsDeleted { get; set; }
    public int HttpDetailsDeleted { get; set; }
    public int ExceptionDetailsDeleted { get; set; }
    public int TraceEventsDeleted { get; set; }
    public int OrphanedTracesDeleted { get; set; }
    public int ClusterEventsDeleted { get; set; }
    public int OrphanedClustersDeleted { get; set; }
    public string Message { get; set; } = "";
}
