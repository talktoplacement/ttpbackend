using CareerPlatform.Api.Features.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>Language</c> model config: int PK by convention; the Language→Section
/// relationship is discovered by convention/annotation as in the legacy model (Req 18.1, 18.2).
/// </summary>
public sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.HasKey(l => l.Id);
        builder.Ignore(l => l.DomainEvents);
    }
}
