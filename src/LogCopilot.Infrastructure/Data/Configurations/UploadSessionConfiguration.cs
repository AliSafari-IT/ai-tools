using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
{
    public void Configure(EntityTypeBuilder<UploadSession> builder)
    {
        builder.HasKey(us => us.Id);
        builder.Property(us => us.Name).IsRequired().HasMaxLength(200);
        builder.Property(us => us.Description).HasMaxLength(1000);
        builder.Property(us => us.Status).IsRequired();
        builder.Property(us => us.CreatedAt).IsRequired();
        builder.Property(us => us.UpdatedAt).IsRequired();

        builder.HasOne(us => us.Organization)
            .WithMany(o => o.UploadSessions)
            .HasForeignKey(us => us.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(us => us.OrganizationId);
        builder.HasIndex(us => us.Status);
        builder.HasIndex(us => us.CreatedAt);
    }
}
