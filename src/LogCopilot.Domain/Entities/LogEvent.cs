namespace LogCopilot.Domain.Entities;

public class LogEvent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? LogSourceId { get; set; }
    public Guid UploadSessionId { get; set; }
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string? MessageTemplate { get; set; }
    public string RenderedMessage { get; set; } = string.Empty;
    public string? RawJson { get; set; }
    public string? PropertiesJson { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? ParentSpanId { get; set; }
    public string? RequestId { get; set; }
    public string? CorrelationId { get; set; }
    public string EventHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public LogSource? LogSource { get; set; }
    public HttpEventDetails? HttpDetails { get; set; }
    public ExceptionDetails? ExceptionDetails { get; set; }
    public ICollection<TraceEvent> TraceEvents { get; set; } = new List<TraceEvent>();
    public ICollection<ClusterEvent> ClusterEvents { get; set; } = new List<ClusterEvent>();
}

public enum LogLevel
{
    Verbose = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4,
    Fatal = 5
}
