namespace LogCopilot.Domain.Entities;

public class ExceptionDetails
{
    public Guid Id { get; set; }
    public Guid LogEventId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string SignatureHash { get; set; } = string.Empty;
    public string? InnerSignatureHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public LogEvent LogEvent { get; set; } = null!;
}
