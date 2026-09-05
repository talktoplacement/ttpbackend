using CareerPlatform.Api.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>Order</c> model config verbatim: a composite index on
/// <c>(UserId, Status)</c> for per-user reads and an index on <c>RazorpayOrderId</c> for
/// verification lookups (Req 18.1, 18.2, 24.5).
/// </summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        // Orders: fast lookup of a user's orders and by Razorpay order id (verification).
        builder.HasIndex(o => new { o.UserId, o.Status });
        builder.HasIndex(o => o.RazorpayOrderId);

        builder.Ignore(o => o.DomainEvents);
    }
}
