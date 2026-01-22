namespace LogCopilot.Domain.Entities;

public class IncidentReport
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public ReportScope Scope { get; set; }
    public Guid? ClusterId { get; set; }
    public Guid? TraceId { get; set; }
    public DateTime? TimeRangeStart { get; set; }
    public DateTime? TimeRangeEnd { get; set; }
    public string PromptParams { get; set; } = string.Empty;
    public string OutputJson { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string? RequestedProvider { get; set; }
    public string? ActualProvider { get; set; }
    public string? ProviderStatus { get; set; }
    public string? ProviderErrorSummary { get; set; }
    public string Status { get; set; } = "Completed";
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public Organization Organization { get; set; } = null!;
}

public enum ReportScope
{
    Cluster = 0,
    Trace = 1,
    TimeRange = 2
}
