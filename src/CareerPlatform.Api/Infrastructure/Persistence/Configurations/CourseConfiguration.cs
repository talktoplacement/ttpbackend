using CareerPlatform.Api.Features.Courses.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>Course</c> model config verbatim: unique <c>Slug</c> index and an
/// index on <c>IsPublished</c> for fast public catalog reads (Req 18.1, 18.2, 24.5).
/// </summary>
public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.Id);

        // Catalog: unique slug + index published flag for fast public catalog reads.
        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.IsPublished);

        builder.Ignore(c => c.DomainEvents);
    }
}
