namespace LogCopilot.Domain.Entities;

public class BillingPlan
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal MonthlyPrice { get; set; }
    public int MaxUsers { get; set; }
    public long MaxStorageBytes { get; set; }
    public int MaxUploadSessionsPerMonth { get; set; }
    public bool EnablesAiReports { get; set; }
    public bool EnablesSemanticClustering { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
