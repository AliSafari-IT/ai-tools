namespace LogCopilot.Domain.Entities;

public class IngestionJob
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UploadSessionId { get; set; }
    public Guid UploadedFileId { get; set; }
    public IngestionJobStatus Status { get; set; }
    public int TotalLines { get; set; }
    public int ProcessedLines { get; set; }
    public int SuccessfulLines { get; set; }
    public int FailedLines { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public UploadSession UploadSession { get; set; } = null!;
}

public enum IngestionJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}
