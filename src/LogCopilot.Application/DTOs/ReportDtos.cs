namespace LogCopilot.Application.DTOs;

public class GenerateReportDto
{
    public string Scope { get; set; } = string.Empty;
    public Guid? ClusterId { get; set; }
    public Guid? TraceId { get; set; }
    public DateTime? TimeRangeStart { get; set; }
    public DateTime? TimeRangeEnd { get; set; }
}

public class IncidentReportDto
{
    public Guid Id { get; set; }
    public string Scope { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Summary { get; set; } = string.Empty;
}

public class IncidentReportDetailDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Scope { get; set; } = string.Empty;
    public Guid? ClusterId { get; set; }
    public Guid? TraceId { get; set; }
    public DateTime? TimeRangeStart { get; set; }
    public DateTime? TimeRangeEnd { get; set; }
    public DateTime CreatedAt { get; set; }
    public ReportOutput Output { get; set; } = new();
}

public class ReportOutput
{
    public string ExecutiveSummary { get; set; } = string.Empty;
    public ReportMetrics Metrics { get; set; } = new();
    public List<TopIssue> TopIssues { get; set; } = new();
    public List<RootCauseHypothesis> RootCauseHypotheses { get; set; } = new();
    public List<FixTask> RecommendedFixPlan { get; set; } = new();
    public List<string> ObservabilityGaps { get; set; } = new();
    public List<TimelineHighlight> TimelineHighlights { get; set; } = new();
    public string Provider { get; set; } = "Heuristic";
    public string? RequestedProvider { get; set; }
    public string? ActualProvider { get; set; }
    public string? ProviderStatus { get; set; }
    public string? ProviderErrorSummary { get; set; }
}

public class ReportMetrics
{
    public int TotalEvents { get; set; }
    public int ErrorCount { get; set; }
    public int FatalCount { get; set; }
    public int WarningCount { get; set; }
    public int InfoCount { get; set; }
    public int UniqueClusters { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public double DurationHours { get; set; }
}

public class TopIssue
{
    public Guid? ClusterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public List<string> Evidence { get; set; } = new();
    public List<string> SuggestedActions { get; set; } = new();
}

public class RootCauseHypothesis
{
    public string Issue { get; set; } = string.Empty;
    public string LikelyCause { get; set; } = string.Empty;
    public List<string> SupportingEvidence { get; set; } = new();
    public string Confidence { get; set; } = "Medium";
}

public class FixTask
{
    public string Priority { get; set; } = "P1";
    public string Task { get; set; } = string.Empty;
    public string WhatToChange { get; set; } = string.Empty;
    public string RiskImpact { get; set; } = string.Empty;
    public string HowToVerify { get; set; } = string.Empty;
}

public class TimelineHighlight
{
    public DateTime Timestamp { get; set; }
    public string Event { get; set; } = string.Empty;
    public int EventCount { get; set; }
}

public class EvidenceItem
{
    public Guid EventId { get; set; }
    public int LineNumber { get; set; }
    public string Excerpt { get; set; } = string.Empty;
    public string Relevance { get; set; } = string.Empty;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
