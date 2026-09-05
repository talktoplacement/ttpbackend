using CareerPlatform.Api.Features.Payments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>Transaction</c> model config: int PK by convention; the
/// Transaction→UserProfile relationship is discovered by annotation as in the legacy model
/// (Req 18.1, 18.2).
/// </summary>
public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.HasKey(t => t.Id);

        // Additive: currency snapshot with an INR default (Req 8.4, 13.1).
        builder.Property(t => t.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("INR");

        // Additive: unique filtered index makes a duplicate verified callback a no-op while
        // letting pre-existing rows keep NULL without collision (Req 10.1, 13.1).
        builder.HasIndex(t => t.GatewayOrderId)
            .IsUnique()
            .HasFilter("\"GatewayOrderId\" IS NOT NULL");

        builder.Ignore(t => t.DomainEvents);
    }
}
