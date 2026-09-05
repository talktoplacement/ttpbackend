using CareerPlatform.Api.Features.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="PendingRegistration"/>. Enforces the natural-key uniqueness
/// on <c>Email</c>, sets sensible column lengths, and ignores the in-memory <c>DomainEvents</c>
/// collection so EF does not try to persist it.
/// </summary>
public sealed class PendingRegistrationConfiguration : IEntityTypeConfiguration<PendingRegistration>
{
    public void Configure(EntityTypeBuilder<PendingRegistration> builder)
    {
        builder.ToTable("PendingRegistrations");

        builder.HasKey(p => p.Id);
        builder.HasIndex(p => p.Email).IsUnique();

        // In-memory concern, never persisted (matches every other aggregate in this codebase).
        builder.Ignore(p => p.DomainEvents);
    }
}
