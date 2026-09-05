using CareerPlatform.Api.Features.Plans.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>Plan</c> model config verbatim: an index on <c>IsPublished</c> for
/// fast public catalog reads (Req 18.1, 18.2, 24.5).
/// </summary>
public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.IsPublished);

        builder.Ignore(p => p.DomainEvents);
    }
}
