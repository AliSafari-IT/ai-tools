namespace LogCopilot.Domain.Entities;

public class UploadedFile
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid UploadSessionId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public LogFileType FileType { get; set; }
    public string? ContentHash { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }

    public UploadSession UploadSession { get; set; } = null!;
}

public enum LogFileType
{
    SerilogJsonLines = 0,
    NginxAccessLog = 1,
    NginxErrorLog = 2,
    SerilogPlainText = 3
}
