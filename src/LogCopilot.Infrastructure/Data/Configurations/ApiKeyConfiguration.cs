using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(ak => ak.Id);
        builder.Property(ak => ak.Name).IsRequired().HasMaxLength(200);
        builder.Property(ak => ak.KeyHash).IsRequired();
        builder.Property(ak => ak.KeyPrefix).IsRequired().HasMaxLength(20);
        builder.Property(ak => ak.IsActive).IsRequired();
        builder.Property(ak => ak.CreatedAt).IsRequired();
        builder.Property(ak => ak.UpdatedAt).IsRequired();

        builder.HasIndex(ak => ak.OrganizationId);
        builder.HasIndex(ak => ak.KeyHash).IsUnique();
        builder.HasIndex(ak => ak.IsActive);
    }
}
