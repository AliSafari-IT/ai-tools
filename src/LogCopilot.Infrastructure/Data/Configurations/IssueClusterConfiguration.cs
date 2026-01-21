using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class IssueClusterConfiguration : IEntityTypeConfiguration<IssueCluster>
{
    public void Configure(EntityTypeBuilder<IssueCluster> builder)
    {
        builder.HasKey(ic => ic.Id);
        builder.Property(ic => ic.Type).IsRequired();
        builder.Property(ic => ic.Signature).IsRequired().HasMaxLength(64);
        builder.Property(ic => ic.Title).IsRequired().HasMaxLength(500);
        builder.Property(ic => ic.FirstSeen).IsRequired();
        builder.Property(ic => ic.LastSeen).IsRequired();
        builder.Property(ic => ic.Severity).IsRequired();
        builder.Property(ic => ic.Status).IsRequired();
        builder.Property(ic => ic.CreatedAt).IsRequired();
        builder.Property(ic => ic.UpdatedAt).IsRequired();

        builder.HasOne(ic => ic.Organization)
            .WithMany(o => o.IssueClusters)
            .HasForeignKey(ic => ic.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ic => ic.OrganizationId);
        builder.HasIndex(ic => ic.Signature);
        builder.HasIndex(ic => ic.Status);
        builder.HasIndex(ic => ic.Severity);
        builder.HasIndex(ic => ic.LastSeen);
    }
}
