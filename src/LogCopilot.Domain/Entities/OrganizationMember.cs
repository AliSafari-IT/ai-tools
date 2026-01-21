namespace LogCopilot.Domain.Entities;

public class OrganizationMember
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Role Role { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public Organization Organization { get; set; } = null!;
    public User User { get; set; } = null!;
}

public enum Role
{
    Member = 0,
    Admin = 1
}
