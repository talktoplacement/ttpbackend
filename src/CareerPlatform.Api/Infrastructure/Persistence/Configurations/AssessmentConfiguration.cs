using CareerPlatform.Api.Features.Assessments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("Assessments");
        builder.HasKey(a => a.Id);
        builder.HasIndex(a => a.Slug).IsUnique();
        builder.HasIndex(a => a.Category);
        builder.HasIndex(a => a.IsPublished);
        builder.Ignore(a => a.DomainEvents);
    }
}
