using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LogCopilot.Domain.Entities;

namespace LogCopilot.Infrastructure.Data.Configurations;

public class OrganizationMemberConfiguration : IEntityTypeConfiguration<OrganizationMember>
{
    public void Configure(EntityTypeBuilder<OrganizationMember> builder)
    {
        builder.HasKey(om => om.Id);
        builder.Property(om => om.Role).IsRequired();
        builder.Property(om => om.CreatedAt).IsRequired();
        builder.Property(om => om.UpdatedAt).IsRequired();

        builder.HasOne(om => om.Organization)
            .WithMany(o => o.Members)
            .HasForeignKey(om => om.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(om => om.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(om => om.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(om => new { om.OrganizationId, om.UserId }).IsUnique();
        builder.HasIndex(om => om.UserId);
    }
}
