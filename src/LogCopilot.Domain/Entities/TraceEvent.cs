namespace LogCopilot.Domain.Entities;

public class TraceEvent
{
    public Guid Id { get; set; }
    public Guid RequestTraceId { get; set; }
    public Guid LogEventId { get; set; }
    public int Ordinal { get; set; }
    public DateTime CreatedAt { get; set; }

    public RequestTrace RequestTrace { get; set; } = null!;
    public LogEvent LogEvent { get; set; } = null!;
}
