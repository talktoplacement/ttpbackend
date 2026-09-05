using CareerPlatform.Api.Features.Orders.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>Enrollment</c> model config verbatim: a unique composite index on
/// <c>(UserId, ProductType, ProductId)</c> that both serves per-user reads and defends against
/// duplicate grants from repeated verify callbacks (Req 18.1, 18.2, 24.5).
/// </summary>
public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.HasKey(e => e.Id);

        builder.HasIndex(e => new { e.UserId, e.ProductType, e.ProductId }).IsUnique();

        builder.Ignore(e => e.DomainEvents);
    }
}
