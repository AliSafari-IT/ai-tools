namespace LogCopilot.Infrastructure.Parsers.Models;

public class ParsedLogEvent
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string? MessageTemplate { get; set; }
    public string RenderedMessage { get; set; } = string.Empty;
    public string? RawJson { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? ParentSpanId { get; set; }
    public string? RequestId { get; set; }
    public string? CorrelationId { get; set; }
    public ParsedHttpDetails? HttpDetails { get; set; }
    public ParsedExceptionDetails? ExceptionDetails { get; set; }
}

public class ParsedHttpDetails
{
    public string? Method { get; set; }
    public string? Path { get; set; }
    public string? QueryString { get; set; }
    public int? StatusCode { get; set; }
    public double? DurationMs { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
}

public class ParsedExceptionDetails
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public ParsedExceptionDetails? InnerException { get; set; }
}
