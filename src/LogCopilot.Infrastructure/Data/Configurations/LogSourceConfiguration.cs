using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class LogSourceConfiguration : IEntityTypeConfiguration<LogSource>
{
    public void Configure(EntityTypeBuilder<LogSource> builder)
    {
        builder.HasKey(ls => ls.Id);
        builder.Property(ls => ls.Name).IsRequired().HasMaxLength(200);
        builder.Property(ls => ls.Environment).HasMaxLength(100);
        builder.Property(ls => ls.Version).HasMaxLength(50);
        builder.Property(ls => ls.CreatedAt).IsRequired();
        builder.Property(ls => ls.UpdatedAt).IsRequired();

        builder.HasOne(ls => ls.Organization)
            .WithMany(o => o.LogSources)
            .HasForeignKey(ls => ls.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ls => ls.OrganizationId);
        builder.HasIndex(ls => new { ls.Name, ls.Environment });
    }
}
