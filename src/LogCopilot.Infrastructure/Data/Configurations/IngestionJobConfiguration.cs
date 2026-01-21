using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class IngestionJobConfiguration : IEntityTypeConfiguration<IngestionJob>
{
    public void Configure(EntityTypeBuilder<IngestionJob> builder)
    {
        builder.HasKey(ij => ij.Id);
        builder.Property(ij => ij.Status).IsRequired();
        builder.Property(ij => ij.CreatedAt).IsRequired();
        builder.Property(ij => ij.UpdatedAt).IsRequired();

        builder.HasOne(ij => ij.UploadSession)
            .WithMany(us => us.IngestionJobs)
            .HasForeignKey(ij => ij.UploadSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ij => ij.OrganizationId);
        builder.HasIndex(ij => ij.UploadSessionId);
        builder.HasIndex(ij => ij.Status);
    }
}
