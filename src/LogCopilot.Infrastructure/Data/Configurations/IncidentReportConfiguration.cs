using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class IncidentReportConfiguration : IEntityTypeConfiguration<IncidentReport>
{
    public void Configure(EntityTypeBuilder<IncidentReport> builder)
    {
        builder.HasKey(ir => ir.Id);
        builder.Property(ir => ir.Scope).IsRequired();
        builder.Property(ir => ir.PromptParams).IsRequired();
        builder.Property(ir => ir.OutputJson).IsRequired();
        builder.Property(ir => ir.Version).IsRequired();
        builder.Property(ir => ir.CreatedAt).IsRequired();
        builder.Property(ir => ir.UpdatedAt).IsRequired();

        builder.HasOne(ir => ir.Organization)
            .WithMany(o => o.IncidentReports)
            .HasForeignKey(ir => ir.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ir => ir.OrganizationId);
        builder.HasIndex(ir => ir.ClusterId);
        builder.HasIndex(ir => ir.TraceId);
        builder.HasIndex(ir => ir.CreatedAt);
    }
}
