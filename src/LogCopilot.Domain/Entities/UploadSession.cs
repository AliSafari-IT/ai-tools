namespace LogCopilot.Domain.Entities;

public class UploadSession
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public UploadSessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<UploadedFile> Files { get; set; } = new List<UploadedFile>();
    public ICollection<IngestionJob> IngestionJobs { get; set; } = new List<IngestionJob>();
}

public enum UploadSessionStatus
{
    Created = 0,
    Uploading = 1,
    Ready = 2,
    Processing = 3,
    Completed = 4,
    Failed = 5
}
