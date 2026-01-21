using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class HttpEventDetailsConfiguration : IEntityTypeConfiguration<HttpEventDetails>
{
    public void Configure(EntityTypeBuilder<HttpEventDetails> builder)
    {
        builder.HasKey(hed => hed.Id);
        builder.Property(hed => hed.Method).HasMaxLength(10);
        builder.Property(hed => hed.Path).HasMaxLength(2000);
        builder.Property(hed => hed.ClientIp).HasMaxLength(50);
        builder.Property(hed => hed.CreatedAt).IsRequired();
        builder.Property(hed => hed.UpdatedAt).IsRequired();

        builder.HasOne(hed => hed.LogEvent)
            .WithOne(le => le.HttpDetails)
            .HasForeignKey<HttpEventDetails>(hed => hed.LogEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(hed => hed.LogEventId);
        builder.HasIndex(hed => hed.StatusCode);
        builder.HasIndex(hed => new { hed.Method, hed.Path });
    }
}
