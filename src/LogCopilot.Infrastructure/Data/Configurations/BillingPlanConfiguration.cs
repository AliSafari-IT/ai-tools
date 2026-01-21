using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class BillingPlanConfiguration : IEntityTypeConfiguration<BillingPlan>
{
    public void Configure(EntityTypeBuilder<BillingPlan> builder)
    {
        builder.HasKey(bp => bp.Id);
        builder.Property(bp => bp.Name).IsRequired().HasMaxLength(200);
        builder.Property(bp => bp.Description).HasMaxLength(1000);
        builder.Property(bp => bp.MonthlyPrice).IsRequired().HasPrecision(18, 2);
        builder.Property(bp => bp.IsActive).IsRequired();
        builder.Property(bp => bp.CreatedAt).IsRequired();
        builder.Property(bp => bp.UpdatedAt).IsRequired();

        builder.HasIndex(bp => bp.IsActive);
    }
}
