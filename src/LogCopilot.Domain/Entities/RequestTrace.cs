namespace LogCopilot.Domain.Entities;

public class RequestTrace
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationMs { get; set; }
    public int EventCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int SpanCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public ICollection<TraceEvent> TraceEvents { get; set; } = new List<TraceEvent>();
}
