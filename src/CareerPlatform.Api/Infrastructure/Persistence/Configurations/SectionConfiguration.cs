using CareerPlatform.Api.Features.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>Section</c> model config: int PK by convention; the Section→Language
/// and Section→Topic relationships are discovered by annotation/convention as in the legacy model
/// (Req 18.1, 18.2).
/// </summary>
public sealed class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Ignore(s => s.DomainEvents);
    }
}
