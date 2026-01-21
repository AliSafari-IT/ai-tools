namespace LogCopilot.Application.DTOs;

public class CreateUploadSessionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UploadFileDto
{
    public Stream FileStream { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
}

public class UploadSessionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<UploadedFileDto> Files { get; set; } = new();
    public List<IngestionJobDto> IngestionJobs { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class UploadedFileDto
{
    public Guid Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string FileType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class IngestionJobDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalLines { get; set; }
    public int ProcessedLines { get; set; }
    public int SuccessfulLines { get; set; }
    public int FailedLines { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
