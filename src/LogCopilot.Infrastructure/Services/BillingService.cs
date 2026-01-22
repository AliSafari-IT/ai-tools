using LogCopilot.Application.Interfaces;
using LogCopilot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Infrastructure.Services;

public class BillingService : IBillingService
{
    private readonly LogCopilotDbContext _context;

    public BillingService(LogCopilotDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanAddUserAsync(Guid organizationId)
    {
        var org = await _context
            .Organizations.Include(o => o.BillingPlan)
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org?.BillingPlan == null)
            return true;

        return org.Members.Count < org.BillingPlan.MaxUsers;
    }

    public async Task<bool> CanUploadAsync(Guid organizationId, long fileSizeBytes)
    {
        var org = await _context
            .Organizations.Include(o => o.BillingPlan)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org?.BillingPlan == null)
            return true;

        await ResetMonthlyCountersIfNeededAsync(org);

        var canUploadCount =
            org.CurrentMonthUploadCount < org.BillingPlan.MaxUploadSessionsPerMonth;
        var canUploadSize =
            (org.CurrentStorageBytes + fileSizeBytes) <= org.BillingPlan.MaxStorageBytes;

        return canUploadCount && canUploadSize;
    }

    public async Task IncrementUploadCountAsync(Guid organizationId)
    {
        var org = await _context.Organizations.FindAsync(organizationId);
        if (org == null)
            return;

        await ResetMonthlyCountersIfNeededAsync(org);
        org.CurrentMonthUploadCount++;
        org.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task IncrementStorageAsync(Guid organizationId, long bytes)
    {
        var org = await _context.Organizations.FindAsync(organizationId);
        if (org == null)
            return;

        org.CurrentStorageBytes += bytes;
        org.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<BillingPlanInfo> GetPlanInfoAsync(Guid organizationId)
    {
        var org = await _context
            .Organizations.Include(o => o.BillingPlan)
            .Include(o => o.Members)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org?.BillingPlan == null)
        {
            return new BillingPlanInfo
            {
                PlanName = "Community",
                MaxUsers = 999,
                MaxStorageBytes = long.MaxValue,
                MaxUploadSessionsPerMonth = 999,
                CurrentUsers = org?.Members.Count ?? 0,
                CurrentStorageBytes = org?.CurrentStorageBytes ?? 0,
                CurrentMonthUploads = org?.CurrentMonthUploadCount ?? 0,
                EnablesAiReports = false,
                EnablesSemanticClustering = false,
            };
        }

        return new BillingPlanInfo
        {
            PlanName = org.BillingPlan.Name,
            MaxUsers = org.BillingPlan.MaxUsers,
            MaxStorageBytes = org.BillingPlan.MaxStorageBytes,
            MaxUploadSessionsPerMonth = org.BillingPlan.MaxUploadSessionsPerMonth,
            CurrentUsers = org.Members.Count,
            CurrentStorageBytes = org.CurrentStorageBytes,
            CurrentMonthUploads = org.CurrentMonthUploadCount,
            EnablesAiReports = org.BillingPlan.EnablesAiReports,
            EnablesSemanticClustering = org.BillingPlan.EnablesSemanticClustering,
        };
    }

    public async Task<bool> HasFeatureAsync(Guid organizationId, string feature)
    {
        var org = await _context
            .Organizations.Include(o => o.BillingPlan)
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (org?.BillingPlan == null)
            return false;

        return feature switch
        {
            "ai_reports" => org.BillingPlan.EnablesAiReports,
            "semantic_clustering" => org.BillingPlan.EnablesSemanticClustering,
            _ => false,
        };
    }

    private async Task ResetMonthlyCountersIfNeededAsync(Domain.Entities.Organization org)
    {
        var now = DateTime.UtcNow;
        if (
            org.LastUploadCountReset.Month != now.Month
            || org.LastUploadCountReset.Year != now.Year
        )
        {
            org.CurrentMonthUploadCount = 0;
            org.LastUploadCountReset = now;
            await _context.SaveChangesAsync();
        }
    }
}
