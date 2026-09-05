using CareerPlatform.Api.Features.SubscriptionPlans.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF configuration for the <see cref="SubscriptionPlan"/> catalog table: a unique <c>Code</c>,
/// bounded string lengths, decimal precision, an int-mapped billing unit, and an index on
/// <c>IsActive</c> for fast catalog reads (Req 1.1, 1.4, 1.5, 6, 14.5).
/// </summary>
public sealed class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code).IsRequired().HasMaxLength(64);
        builder.HasIndex(p => p.Code).IsUnique();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);

        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("INR");

        builder.Property(p => p.Price).HasPrecision(18, 2);

        builder.Property(p => p.IntervalUnit).HasConversion<int>();

        builder.HasIndex(p => p.IsActive);

        builder.Ignore(p => p.DomainEvents);
    }
}
