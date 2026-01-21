namespace LogCopilot.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public ICollection<OrganizationMember> Members { get; set; } = new List<OrganizationMember>();
    public ICollection<UploadSession> UploadSessions { get; set; } = new List<UploadSession>();
    public ICollection<LogSource> LogSources { get; set; } = new List<LogSource>();
    public ICollection<IssueCluster> IssueClusters { get; set; } = new List<IssueCluster>();
    public ICollection<IncidentReport> IncidentReports { get; set; } = new List<IncidentReport>();
}
