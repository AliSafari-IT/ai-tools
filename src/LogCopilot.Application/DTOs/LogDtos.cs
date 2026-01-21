namespace LogCopilot.Application.DTOs;

public class LogQueryDto
{
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string? Level { get; set; }
    public string? SearchText { get; set; }
    public string? TraceId { get; set; }
    public string? CorrelationId { get; set; }
    public int? StatusCode { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class LogEventDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string RenderedMessage { get; set; } = string.Empty;
    public string? TraceId { get; set; }
    public string? Method { get; set; }
    public string? Path { get; set; }
    public int? StatusCode { get; set; }
    public double? DurationMs { get; set; }
}

public class LogEventDetailDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = string.Empty;
    public string? MessageTemplate { get; set; }
    public string RenderedMessage { get; set; } = string.Empty;
    public string? RawJson { get; set; }
    public string? PropertiesJson { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? ParentSpanId { get; set; }
    public string? RequestId { get; set; }
    public string? CorrelationId { get; set; }
    public HttpDetailsDto? HttpDetails { get; set; }
    public ExceptionDetailsDto? ExceptionDetails { get; set; }
}

public class HttpDetailsDto
{
    public string? Method { get; set; }
    public string? Path { get; set; }
    public string? QueryString { get; set; }
    public int? StatusCode { get; set; }
    public double? DurationMs { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
}

public class ExceptionDetailsDto
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
}
