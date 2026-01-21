using LogCopilot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class RequestTraceConfiguration : IEntityTypeConfiguration<RequestTrace>
{
    public void Configure(EntityTypeBuilder<RequestTrace> builder)
    {
        builder.HasKey(rt => rt.Id);
        builder.Property(rt => rt.StartTime).IsRequired();
        builder.Property(rt => rt.EndTime).IsRequired();
        builder.Property(rt => rt.CreatedAt).IsRequired();
        builder.Property(rt => rt.UpdatedAt).IsRequired();

        builder.HasIndex(rt => rt.OrganizationId);
        builder.HasIndex(rt => rt.TraceId);
        builder.HasIndex(rt => rt.CorrelationId);
        builder.HasIndex(rt => rt.RequestId);
        builder.HasIndex(rt => rt.StartTime);
    }
}
