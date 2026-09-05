using CareerPlatform.Api.Features.Reviews.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class CourseReviewConfiguration : IEntityTypeConfiguration<CourseReview>
{
    public void Configure(EntityTypeBuilder<CourseReview> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        b.Property(x => x.Comment).IsRequired().HasMaxLength(2000);
        b.Property(x => x.Status).IsRequired().HasMaxLength(16);
        b.Property(x => x.ModeratedBy).HasMaxLength(64);
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.CourseId);
        b.HasIndex(x => new { x.UserId, x.CourseId }).IsUnique();
    }
}
