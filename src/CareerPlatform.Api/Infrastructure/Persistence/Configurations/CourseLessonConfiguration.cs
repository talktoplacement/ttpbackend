using CareerPlatform.Api.Features.CourseLessons.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class CourseLessonConfiguration : IEntityTypeConfiguration<CourseLesson>
{
    public void Configure(EntityTypeBuilder<CourseLesson> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.LessonType).IsRequired().HasMaxLength(16);
        b.Property(x => x.ContentUrl).HasMaxLength(1000);
        b.Property(x => x.ContentMarkdown).HasMaxLength(8000);
        b.HasIndex(x => new { x.CourseId, x.OrderIndex });
    }
}
