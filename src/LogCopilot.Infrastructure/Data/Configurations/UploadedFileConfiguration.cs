using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
{
    public void Configure(EntityTypeBuilder<UploadedFile> builder)
    {
        builder.HasKey(uf => uf.Id);
        builder.Property(uf => uf.OriginalFileName).IsRequired().HasMaxLength(500);
        builder.Property(uf => uf.StoragePath).IsRequired().HasMaxLength(1000);
        builder.Property(uf => uf.SizeBytes).IsRequired();
        builder.Property(uf => uf.FileType).IsRequired();
        builder.Property(uf => uf.CreatedAt).IsRequired();
        builder.Property(uf => uf.UpdatedAt).IsRequired();

        builder.HasOne(uf => uf.UploadSession)
            .WithMany(us => us.Files)
            .HasForeignKey(uf => uf.UploadSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(uf => uf.OrganizationId);
        builder.HasIndex(uf => uf.UploadSessionId);
        builder.HasIndex(uf => uf.ContentHash);
    }
}
