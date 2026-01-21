namespace LogCopilot.Application.DTOs;

public class ClusterQueryDto
{
    public string? Status { get; set; }
    public string? Severity { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class IssueClusterDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int EventCount { get; set; }
}

public class ClusterDetailDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public List<LogEventDto> RecentEvents { get; set; } = new();
}
