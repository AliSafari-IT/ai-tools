namespace LogCopilot.Application.DTOs;

public class TraceQueryDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? MinErrorCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class RequestTraceDto
{
    public Guid Id { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationMs { get; set; }
    public int EventCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
}

public class TraceDetailDto
{
    public Guid Id { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double DurationMs { get; set; }
    public int EventCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
    public int SpanCount { get; set; }
    public List<TraceEventItemDto> Events { get; set; } = new();
}

public class TraceEventItemDto
{
    public Guid LogEventId { get; set; }
    public int Ordinal { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string RenderedMessage { get; set; } = string.Empty;
    public string? SpanId { get; set; }
    public double? DurationMs { get; set; }
}
