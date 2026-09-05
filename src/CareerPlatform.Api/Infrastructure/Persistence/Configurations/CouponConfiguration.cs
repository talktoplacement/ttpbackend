using CareerPlatform.Api.Features.Coupons.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Code).IsRequired().HasMaxLength(64);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.DiscountType).IsRequired().HasMaxLength(16);
        b.Property(x => x.DiscountValue).HasColumnType("numeric(12,2)");
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.IsActive);
    }
}
