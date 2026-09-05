using CareerPlatform.Api.Features.Products.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Code).IsRequired().HasMaxLength(64);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.ProductType).IsRequired().HasMaxLength(32);
        b.Property(x => x.Currency).IsRequired().HasMaxLength(8);
        b.Property(x => x.Price).HasColumnType("numeric(12,2)");
        b.HasIndex(x => x.Code).IsUnique();
    }
}
