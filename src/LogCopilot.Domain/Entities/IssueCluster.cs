namespace LogCopilot.Domain.Entities;

public class IssueCluster
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public ClusterType Type { get; set; }
    public string Signature { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public IssueSeverity Severity { get; set; }
    public IssueStatus Status { get; set; }
    public string? Tags { get; set; }
    public int EventCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<ClusterEvent> ClusterEvents { get; set; } = new List<ClusterEvent>();
}

public enum ClusterType
{
    Exception = 0,
    EndpointError = 1,
    Spike = 2
}

public enum IssueSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum IssueStatus
{
    Open = 0,
    Investigating = 1,
    Resolved = 2,
    Ignored = 3
}
