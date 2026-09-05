using CareerPlatform.Api.Features.Practice.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class PracticeQuestionConfiguration : IEntityTypeConfiguration<PracticeQuestion>
{
    public void Configure(EntityTypeBuilder<PracticeQuestion> builder)
    {
        builder.ToTable("PracticeQuestions");
        builder.HasKey(q => q.Id);
        builder.HasIndex(q => q.Slug).IsUnique();
        builder.HasIndex(q => q.Category);
        builder.HasIndex(q => q.IsPublished);
        builder.Ignore(q => q.DomainEvents);
    }
}
