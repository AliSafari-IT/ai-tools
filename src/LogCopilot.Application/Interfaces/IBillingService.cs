namespace LogCopilot.Application.Interfaces;

public interface IBillingService
{
    Task<bool> CanAddUserAsync(Guid organizationId);
    Task<bool> CanUploadAsync(Guid organizationId, long fileSizeBytes);
    Task IncrementUploadCountAsync(Guid organizationId);
    Task IncrementStorageAsync(Guid organizationId, long bytes);
    Task<BillingPlanInfo> GetPlanInfoAsync(Guid organizationId);
    Task<bool> HasFeatureAsync(Guid organizationId, string feature);
}

public class BillingPlanInfo
{
    public string PlanName { get; set; } = "Community";
    public int MaxUsers { get; set; }
    public long MaxStorageBytes { get; set; }
    public int MaxUploadSessionsPerMonth { get; set; }
    public int CurrentUsers { get; set; }
    public long CurrentStorageBytes { get; set; }
    public int CurrentMonthUploads { get; set; }
    public bool EnablesAiReports { get; set; }
    public bool EnablesSemanticClustering { get; set; }
}
