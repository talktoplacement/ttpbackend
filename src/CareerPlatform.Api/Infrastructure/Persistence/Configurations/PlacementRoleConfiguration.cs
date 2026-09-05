using CareerPlatform.Api.Features.PlacementRoles.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class PlacementRoleConfiguration : IEntityTypeConfiguration<PlacementRole>
{
    public void Configure(EntityTypeBuilder<PlacementRole> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Slug).IsRequired().HasMaxLength(64);
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.AvgCtcRange).HasMaxLength(64);
        b.Property(x => x.RequirementsMarkdown).HasMaxLength(4000);
        b.HasIndex(x => x.Slug).IsUnique();
    }
}
