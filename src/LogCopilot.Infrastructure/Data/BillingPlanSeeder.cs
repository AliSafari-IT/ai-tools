using LogCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogCopilot.Infrastructure.Data;

public static class BillingPlanSeeder
{
    public static async Task SeedBillingPlansAsync(LogCopilotDbContext context)
    {
        if (await context.BillingPlans.AnyAsync())
        {
            return;
        }

        var communityPlan = new BillingPlan
        {
            Id = Guid.NewGuid(),
            Name = "Community",
            Description = "Free community plan with basic features",
            MaxUsers = 5,
            MaxStorageBytes = 1L * 1024 * 1024 * 1024,
            MaxUploadSessionsPerMonth = 50,
            EnablesAiReports = false,
            EnablesSemanticClustering = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var proPlan = new BillingPlan
        {
            Id = Guid.NewGuid(),
            Name = "Pro",
            Description = "Professional plan with AI-powered reports",
            MaxUsers = 50,
            MaxStorageBytes = 100L * 1024 * 1024 * 1024,
            MaxUploadSessionsPerMonth = 1000,
            EnablesAiReports = true,
            EnablesSemanticClustering = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.BillingPlans.AddRange(communityPlan, proPlan);
        await context.SaveChangesAsync();
    }
}
