using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class TraceEventConfiguration : IEntityTypeConfiguration<TraceEvent>
{
    public void Configure(EntityTypeBuilder<TraceEvent> builder)
    {
        builder.HasKey(te => te.Id);
        builder.Property(te => te.Ordinal).IsRequired();
        builder.Property(te => te.CreatedAt).IsRequired();

        builder.HasOne(te => te.RequestTrace)
            .WithMany(rt => rt.TraceEvents)
            .HasForeignKey(te => te.RequestTraceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(te => te.LogEvent)
            .WithMany(le => le.TraceEvents)
            .HasForeignKey(te => te.LogEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(te => te.RequestTraceId);
        builder.HasIndex(te => te.LogEventId);
        builder.HasIndex(te => new { te.RequestTraceId, te.Ordinal });
    }
}
