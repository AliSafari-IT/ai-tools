using LogCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class ExceptionDetailsConfiguration : IEntityTypeConfiguration<ExceptionDetails>
{
    public void Configure(EntityTypeBuilder<ExceptionDetails> builder)
    {
        builder.HasKey(ed => ed.Id);
        builder.Property(ed => ed.Type).IsRequired().HasMaxLength(500);
        builder.Property(ed => ed.Message).IsRequired();
        builder.Property(ed => ed.SignatureHash).IsRequired().HasMaxLength(64);
        builder.Property(ed => ed.InnerSignatureHash).HasMaxLength(64);
        builder.Property(ed => ed.CreatedAt).IsRequired();
        builder.Property(ed => ed.UpdatedAt).IsRequired();

        builder
            .HasOne(ed => ed.LogEvent)
            .WithOne(le => le.ExceptionDetails)
            .HasForeignKey<ExceptionDetails>(ed => ed.LogEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ed => ed.LogEventId);
        builder.HasIndex(ed => ed.SignatureHash);
        builder.HasIndex(ed => ed.Type);
    }
}
