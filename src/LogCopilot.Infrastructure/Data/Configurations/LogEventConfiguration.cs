using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class LogEventConfiguration : IEntityTypeConfiguration<LogEvent>
{
    public void Configure(EntityTypeBuilder<LogEvent> builder)
    {
        builder.HasKey(le => le.Id);
        builder.Property(le => le.Timestamp).IsRequired();
        builder.Property(le => le.Level).IsRequired();
        builder.Property(le => le.RenderedMessage).IsRequired();
        builder.Property(le => le.EventHash).IsRequired().HasMaxLength(64);
        builder.Property(le => le.CreatedAt).IsRequired();
        builder.Property(le => le.UpdatedAt).IsRequired();

        builder.Property(le => le.PropertiesJson).HasColumnType("text");
        builder.Property(le => le.RawJson).HasColumnType("text");

        builder.HasOne(le => le.LogSource)
            .WithMany(ls => ls.LogEvents)
            .HasForeignKey(le => le.LogSourceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(le => le.OrganizationId);
        builder.HasIndex(le => le.Timestamp);
        builder.HasIndex(le => le.Level);
        builder.HasIndex(le => le.TraceId);
        builder.HasIndex(le => le.CorrelationId);
        builder.HasIndex(le => le.RequestId);
        builder.HasIndex(le => le.EventHash);
        builder.HasIndex(le => le.UploadSessionId);
    }
}
