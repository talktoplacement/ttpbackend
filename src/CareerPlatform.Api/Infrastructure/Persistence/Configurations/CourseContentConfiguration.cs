using CareerPlatform.Api.Features.Content.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>CourseContent</c> model config: int PK by convention. No extra Fluent
/// rules existed in the legacy <c>OnModelCreating</c> (Req 18.1, 18.2, 24.5).
/// </summary>
public sealed class CourseContentConfiguration : IEntityTypeConfiguration<CourseContent>
{
    public void Configure(EntityTypeBuilder<CourseContent> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Ignore(c => c.DomainEvents);
    }
}
