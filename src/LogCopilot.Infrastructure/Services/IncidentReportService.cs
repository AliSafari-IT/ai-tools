using System.Text.Json;
using LogCopilot.Application.DTOs;
using LogCopilot.Application.Interfaces;
using LogCopilot.Domain.Entities;
using LogCopilot.Infrastructure.AI;
using LogCopilot.Infrastructure.Analysis;
using LogCopilot.Infrastructure.Data;
using LogCopilot.Infrastructure.Features;
using LogCopilot.Infrastructure.Licensing;
using LogCopilot.Infrastructure.Plugins;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LogCopilot.Infrastructure.Services;

public class IncidentReportService : IIncidentReportService
{
    private readonly LogCopilotDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAIProvider _aiProvider;
    private readonly ILogger<IncidentReportService> _logger;
    private readonly IIncidentNarrativeGenerator _narrativeGenerator;
    private readonly ILicenseVerifier _licenseVerifier;
    private readonly IFeatureFlagService _featureFlags;
    private readonly IConfiguration _configuration;

    public IncidentReportService(
        LogCopilotDbContext context,
        IHttpContextAccessor httpContextAccessor,
        IAIProvider aiProvider,
        ILogger<IncidentReportService> logger,
        IIncidentNarrativeGenerator narrativeGenerator,
        ILicenseVerifier licenseVerifier,
        IFeatureFlagService featureFlags,
        IConfiguration configuration)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _aiProvider = aiProvider;
        _logger = logger;
        _narrativeGenerator = narrativeGenerator;
        _licenseVerifier = licenseVerifier;
        _featureFlags = featureFlags;
        _configuration = configuration;
    }

    public async Task<IncidentReportDto> GenerateReportAsync(GenerateReportDto dto)
    {
        var organizationId = GetOrganizationId();
        var userId = GetUserId();

        ReportScope scope;
        if (!Enum.TryParse<ReportScope>(dto.Scope, true, out scope))
        {
            scope = ReportScope.TimeRange;
        }

        var events = await CollectEventsForReportAsync(organizationId, dto);
        var output = await GenerateReportOutputAsync(events, scope, dto);

        var report = new IncidentReport
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Scope = scope,
            ClusterId = dto.ClusterId,
            TraceId = dto.TraceId,
            TimeRangeStart = dto.TimeRangeStart ?? DateTime.UtcNow.AddHours(-1),
            TimeRangeEnd = dto.TimeRangeEnd ?? DateTime.UtcNow,
            PromptParams = JsonSerializer.Serialize(dto),
            OutputJson = JsonSerializer.Serialize(output),
            Summary = output.ExecutiveSummary,
            Provider = output.Provider,
            Status = "Completed",
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            UpdatedBy = userId
        };

        _context.IncidentReports.Add(report);
        await _context.SaveChangesAsync();

        return new IncidentReportDto
        {
            Id = report.Id,
            Scope = report.Scope.ToString(),
            CreatedAt = report.CreatedAt,
            Summary = output.ExecutiveSummary
        };
    }

    public async Task<PagedResult<IncidentReportDto>> GetReportsAsync(int page, int pageSize)
    {
        var organizationId = GetOrganizationId();

        var totalCount = await _context.IncidentReports
            .Where(r => r.OrganizationId == organizationId)
            .CountAsync();

        var reports = await _context.IncidentReports
            .Where(r => r.OrganizationId == organizationId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var dtos = reports.Select(r =>
        {
            return new IncidentReportDto
            {
                Id = r.Id,
                Scope = r.Scope.ToString(),
                CreatedAt = r.CreatedAt,
                Summary = !string.IsNullOrEmpty(r.Summary) ? r.Summary : "Report generated"
            };
        }).ToList();

        return new PagedResult<IncidentReportDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IncidentReportDetailDto> GetReportByIdAsync(Guid id)
    {
        var organizationId = GetOrganizationId();

        var report = await _context.IncidentReports
            .Where(r => r.Id == id && r.OrganizationId == organizationId)
            .FirstOrDefaultAsync();

        if (report == null)
            throw new Exception("Report not found");

        var output = JsonSerializer.Deserialize<ReportOutput>(report.OutputJson) ?? new ReportOutput();

        return new IncidentReportDetailDto
        {
            Id = report.Id,
            Scope = report.Scope.ToString(),
            ClusterId = report.ClusterId,
            TraceId = report.TraceId,
            TimeRangeStart = report.TimeRangeStart,
            TimeRangeEnd = report.TimeRangeEnd,
            CreatedAt = report.CreatedAt,
            Output = output
        };
    }

    private async Task<List<LogEvent>> CollectEventsForReportAsync(Guid organizationId, GenerateReportDto dto)
    {
        var query = _context.LogEvents
            .Include(e => e.ExceptionDetails)
            .Include(e => e.HttpDetails)
            .Include(e => e.ClusterEvents)
            .Where(e => e.OrganizationId == organizationId);

        if (dto.ClusterId.HasValue)
        {
            query = query.Where(e => e.ClusterEvents.Any(ce => ce.IssueClusterId == dto.ClusterId.Value));
        }
        else if (dto.TraceId.HasValue)
        {
            var trace = await _context.RequestTraces.FindAsync(dto.TraceId.Value);
            if (trace != null)
            {
                query = query.Where(e => e.TraceId == trace.TraceId || e.CorrelationId == trace.CorrelationId || e.RequestId == trace.RequestId);
            }
        }
        else
        {
            var startTime = dto.TimeRangeStart ?? DateTime.UtcNow.AddHours(-1);
            var endTime = dto.TimeRangeEnd ?? DateTime.UtcNow;
            query = query.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime);
        }

        return await query
            .OrderBy(e => e.Timestamp)
            .Take(500)
            .ToListAsync();
    }

    private async Task<ReportOutput> GenerateReportOutputAsync(List<LogEvent> events, ReportScope scope, GenerateReportDto dto)
    {
        return await _narrativeGenerator.GenerateNarrativeAsync(events, scope, dto);
    }

    private async Task<ReportOutput> GenerateComprehensiveReportAsync(List<LogEvent> events, ReportScope scope, GenerateReportDto dto)
    {
        var organizationId = GetOrganizationId();
        
        var metrics = new ReportMetrics
        {
            TotalEvents = events.Count,
            ErrorCount = events.Count(e => e.Level >= Domain.Entities.LogLevel.Error),
            FatalCount = events.Count(e => e.Level == Domain.Entities.LogLevel.Fatal),
            WarningCount = events.Count(e => e.Level == Domain.Entities.LogLevel.Warning),
            InfoCount = events.Count(e => e.Level == Domain.Entities.LogLevel.Information || e.Level == Domain.Entities.LogLevel.Debug || e.Level == Domain.Entities.LogLevel.Verbose),
            FirstSeen = events.Any() ? events.Min(e => e.Timestamp) : DateTime.UtcNow,
            LastSeen = events.Any() ? events.Max(e => e.Timestamp) : DateTime.UtcNow
        };
        metrics.DurationHours = (metrics.LastSeen - metrics.FirstSeen).TotalHours;

        var clusterIds = events
            .SelectMany(e => e.ClusterEvents.Select(ce => ce.IssueClusterId))
            .Distinct()
            .ToList();

        var clusters = await _context.IssueClusters
            .Where(c => clusterIds.Contains(c.Id))
            .OrderByDescending(c => c.Severity)
            .ThenByDescending(c => c.EventCount)
            .Take(5)
            .ToListAsync();

        metrics.UniqueClusters = clusters.Count;

        var topIssues = clusters.Select(cluster =>
        {
            var clusterEvents = events.Where(e => e.ClusterEvents.Any(ce => ce.IssueClusterId == cluster.Id)).ToList();
            var evidence = clusterEvents.Take(3).Select(e => IssuePatternMatcher.RedactSecrets(e.RenderedMessage.Length > 150 ? e.RenderedMessage.Substring(0, 150) + "..." : e.RenderedMessage)).ToList();

            var actions = new List<string>();
            var title = cluster.Title.ToLower();
            if (title.Contains("jwt") || title.Contains("token"))
                actions = new List<string> { "Review authentication middleware configuration", "Validate token format and expiration settings", "Check for proper Bearer token handling" };
            else if (title.Contains("validation"))
                actions = new List<string> { "Review API input validation rules", "Ensure validation error messages are clear", "Update API documentation if needed" };
            else if (title.Contains("database") || title.Contains("connection"))
                actions = new List<string> { "Check database connectivity and health", "Review connection pool settings", "Verify network connectivity to database" };
            else
                actions = new List<string> { "Review stack trace for root cause", "Add additional logging if needed", "Implement error handling" };

            return new TopIssue
            {
                ClusterId = cluster.Id,
                Title = cluster.Title,
                Severity = cluster.Severity.ToString(),
                Count = cluster.EventCount,
                FirstSeen = cluster.FirstSeen,
                LastSeen = cluster.LastSeen,
                Evidence = evidence,
                SuggestedActions = actions
            };
        }).ToList();

        var hypotheses = IssuePatternMatcher.AnalyzePatterns(events, clusters);
        var fixPlan = IssuePatternMatcher.GenerateFixPlan(hypotheses, metrics);
        var observabilityGaps = IssuePatternMatcher.DetectObservabilityGaps(events, metrics);

        var timeline = events
            .GroupBy(e => new DateTime(e.Timestamp.Year, e.Timestamp.Month, e.Timestamp.Day, e.Timestamp.Hour, 0, 0))
            .OrderBy(g => g.Key)
            .Select(g => new TimelineHighlight
            {
                Timestamp = g.Key,
                Event = $"{g.Count(e => e.Level >= Domain.Entities.LogLevel.Error)} errors, {g.Count(e => e.Level == Domain.Entities.LogLevel.Warning)} warnings",
                EventCount = g.Count()
            })
            .Where(t => t.EventCount > 5)
            .Take(10)
            .ToList();

        var healthStatus = metrics.ErrorCount > 100 ? "CRITICAL" :
                          metrics.ErrorCount > 50 ? "DEGRADED" :
                          metrics.ErrorCount > 10 ? "WARNING" : "HEALTHY";

        var executiveSummary = $"System health: {healthStatus}. Analyzed {metrics.TotalEvents} log events over {metrics.DurationHours:F1} hours ({metrics.FirstSeen:g} to {metrics.LastSeen:g}). " +
                             $"Detected {metrics.ErrorCount} errors, {metrics.FatalCount} fatal events, and {metrics.WarningCount} warnings across {metrics.UniqueClusters} distinct issue clusters. ";

        if (metrics.ErrorCount > 50)
            executiveSummary += $"HIGH PRIORITY: Error volume exceeds normal thresholds ({metrics.ErrorCount} errors detected). Immediate investigation recommended. ";
        
        if (topIssues.Any())
        {
            var topCluster = topIssues.First();
            executiveSummary += $"Top issue: {topCluster.Title} ({topCluster.Count} occurrences, {topCluster.Severity} severity). ";
        }

        if (observabilityGaps.Any())
            executiveSummary += $"Observability gaps detected: trace correlation is limited, affecting request flow analysis.";

        return new ReportOutput
        {
            ExecutiveSummary = executiveSummary,
            Metrics = metrics,
            TopIssues = topIssues,
            RootCauseHypotheses = hypotheses,
            RecommendedFixPlan = fixPlan,
            ObservabilityGaps = observabilityGaps,
            TimelineHighlights = timeline,
            Provider = "Heuristic"
        };
    }

    private string BuildReportPrompt(List<LogEvent> events, int errorCount, int warningCount, ReportScope scope)
    {
        var logSample = string.Join("\n", events.Take(20).Select(e => 
            $"[{e.Timestamp:yyyy-MM-dd HH:mm:ss}] [{e.Level}] {e.RenderedMessage}"));

        return $@"Analyze the following log events and generate an incident report in JSON format.

Total Events: {events.Count}
Errors: {errorCount}
Warnings: {warningCount}
Scope: {scope}

Log Sample:
{logSample}

Generate a JSON object with this structure:
{{
  ""Summary"": ""Brief executive summary"",
  ""SuspectedCauses"": [""cause1"", ""cause2""],
  ""Evidence"": [],
  ""Impact"": ""Impact description"",
  ""RecommendedActions"": [""action1"", ""action2""],
  ""Followups"": [""followup1""],
  ""Confidence"": ""High/Medium/Low""
}}";
    }

    private Guid GetOrganizationId()
    {
        var orgIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst("OrganizationId")?.Value;
        if (string.IsNullOrEmpty(orgIdClaim))
            throw new UnauthorizedAccessException("Organization ID not found");
        return Guid.Parse(orgIdClaim);
    }

    private Guid GetUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim))
            throw new UnauthorizedAccessException("User ID not found");
        return Guid.Parse(userIdClaim);
    }
}
