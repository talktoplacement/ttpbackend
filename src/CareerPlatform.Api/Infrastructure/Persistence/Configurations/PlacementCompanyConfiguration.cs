using CareerPlatform.Api.Features.PlacementCompanies.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class PlacementCompanyConfiguration : IEntityTypeConfiguration<PlacementCompany>
{
    public void Configure(EntityTypeBuilder<PlacementCompany> builder)
    {
        builder.ToTable("PlacementCompanies");
        builder.HasKey(c => c.Id);
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.Tier);
        builder.HasIndex(c => c.IsPublished);
        builder.Ignore(c => c.DomainEvents);
    }
}
