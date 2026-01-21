namespace LogCopilot.Domain.Entities;

public class LogSource
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Environment { get; set; }
    public string? Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<LogEvent> LogEvents { get; set; } = new List<LogEvent>();
}
