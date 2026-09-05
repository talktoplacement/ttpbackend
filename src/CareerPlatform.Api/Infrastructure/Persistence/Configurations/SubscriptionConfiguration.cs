using CareerPlatform.Api.Features.SubscriptionPlans.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF configuration for the provisioned <see cref="Subscription"/> table: int-mapped status,
/// decimal precision on the frozen price, foreign keys to <c>UserProfile</c>, <c>SubscriptionPlan</c>,
/// and <c>Transaction</c> with <see cref="DeleteBehavior.Restrict"/>, and a
/// <c>(StudentId, Status)</c> index for active-subscription lookups (Req 9.5, 11.4, 13.2, 14.5).
/// </summary>
public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status).HasConversion<int>();

        builder.Property(s => s.PricePaid).HasPrecision(18, 2);

        builder.Property(s => s.Currency).IsRequired().HasMaxLength(3);

        builder.HasOne(s => s.Student)
            .WithMany()
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Plan)
            .WithMany()
            .HasForeignKey(s => s.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Transaction)
            .WithMany()
            .HasForeignKey(s => s.TransactionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.StudentId, s.Status });

        builder.Ignore(s => s.DomainEvents);
    }
}
