using CareerPlatform.Api.Features.Broadcasts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF configuration for <see cref="Broadcast"/>. Indexed on <c>SentAtUtc</c> descending for the
/// admin history read, bounded lengths on all text columns, and an int-mapped enum. Domain events
/// are in-memory only.
/// </summary>
public sealed class BroadcastConfiguration : IEntityTypeConfiguration<Broadcast>
{
    public void Configure(EntityTypeBuilder<Broadcast> builder)
    {
        builder.ToTable("Broadcasts");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.BroadcastType).HasConversion<int>();
        builder.Property(b => b.Heading).IsRequired().HasMaxLength(200);
        builder.Property(b => b.TargetPlan).HasMaxLength(64);
        builder.Property(b => b.QuestionText).HasMaxLength(2000);
        builder.Property(b => b.QuestionLink).HasMaxLength(500);
        builder.Property(b => b.Message).IsRequired().HasMaxLength(4000);

        builder.HasIndex(b => b.SentAtUtc);
        builder.HasIndex(b => b.BroadcastType);

        builder.Ignore(b => b.DomainEvents);
    }
}
