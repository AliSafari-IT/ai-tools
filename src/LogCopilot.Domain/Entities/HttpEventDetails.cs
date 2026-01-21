namespace LogCopilot.Domain.Entities;

public class HttpEventDetails
{
    public Guid Id { get; set; }
    public Guid LogEventId { get; set; }
    public string? Method { get; set; }
    public string? Path { get; set; }
    public string? QueryString { get; set; }
    public int? StatusCode { get; set; }
    public double? DurationMs { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public LogEvent LogEvent { get; set; } = null!;
}
