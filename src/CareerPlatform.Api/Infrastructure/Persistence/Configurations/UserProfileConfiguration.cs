using CareerPlatform.Api.Features.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>UserProfile</c> model config verbatim: string PK that is NOT
/// database-generated so it can carry the Supabase Auth UUID (Req 18.1, 18.2, 24.5).
/// </summary>
public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasKey(u => u.Id);

        // To ensure compatibility with Supabase's auth.users UUIDs.
        builder.Property(u => u.Id).ValueGeneratedNever();

        // Domain events are an in-memory concern, never persisted.
        builder.Ignore(u => u.DomainEvents);
    }
}
