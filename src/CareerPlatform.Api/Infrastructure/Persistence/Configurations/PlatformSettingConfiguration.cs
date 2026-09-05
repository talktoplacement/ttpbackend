using CareerPlatform.Api.Features.Settings.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF configuration for <see cref="PlatformSetting"/>: a unique natural key on <c>Key</c>, an
/// index on <c>Category</c> for the per-page filter, an int-mapped value type, and bounded string
/// lengths. Domain events are an in-memory concern and never persisted.
/// </summary>
public sealed class PlatformSettingConfiguration : IEntityTypeConfiguration<PlatformSetting>
{
    public void Configure(EntityTypeBuilder<PlatformSetting> builder)
    {
        builder.ToTable("PlatformSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key).IsRequired().HasMaxLength(128);
        builder.HasIndex(s => s.Key).IsUnique();

        builder.Property(s => s.Category).IsRequired().HasMaxLength(64);
        builder.HasIndex(s => s.Category);

        builder.Property(s => s.Label).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Description).HasMaxLength(500);
        builder.Property(s => s.Value).HasMaxLength(2000);
        builder.Property(s => s.ValueType).HasConversion<int>();

        builder.Ignore(s => s.DomainEvents);
    }
}
