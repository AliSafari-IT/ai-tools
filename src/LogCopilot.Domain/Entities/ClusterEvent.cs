namespace LogCopilot.Domain.Entities;

public class ClusterEvent
{
    public Guid Id { get; set; }
    public Guid IssueClusterId { get; set; }
    public Guid LogEventId { get; set; }
    public DateTime CreatedAt { get; set; }

    public IssueCluster IssueCluster { get; set; } = null!;
    public LogEvent LogEvent { get; set; } = null!;
}
