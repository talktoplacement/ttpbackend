using CareerPlatform.Api.Features.Interviews.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class InterviewQuestionConfiguration : IEntityTypeConfiguration<InterviewQuestion>
{
    public void Configure(EntityTypeBuilder<InterviewQuestion> builder)
    {
        builder.ToTable("InterviewQuestions");
        builder.HasKey(q => q.Id);
        builder.HasIndex(q => q.Slug).IsUnique();
        builder.HasIndex(q => q.Topic);
        builder.HasIndex(q => q.IsPublished);
        builder.Ignore(q => q.DomainEvents);
    }
}
