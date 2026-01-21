using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class ClusterEventConfiguration : IEntityTypeConfiguration<ClusterEvent>
{
    public void Configure(EntityTypeBuilder<ClusterEvent> builder)
    {
        builder.HasKey(ce => ce.Id);
        builder.Property(ce => ce.CreatedAt).IsRequired();

        builder.HasOne(ce => ce.IssueCluster)
            .WithMany(ic => ic.ClusterEvents)
            .HasForeignKey(ce => ce.IssueClusterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ce => ce.LogEvent)
            .WithMany(le => le.ClusterEvents)
            .HasForeignKey(ce => ce.LogEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ce => ce.IssueClusterId);
        builder.HasIndex(ce => ce.LogEventId);
    }
}
