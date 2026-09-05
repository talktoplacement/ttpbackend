using CareerPlatform.Api.Features.Assessments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class AssessmentQuestionConfiguration : IEntityTypeConfiguration<AssessmentQuestion>
{
    public void Configure(EntityTypeBuilder<AssessmentQuestion> builder)
    {
        builder.ToTable("AssessmentQuestions");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        // Lengths mirror schema.sql so an over-long value is rejected by EF rather than reaching
        // Postgres and surfacing as a 500.
        builder.Property(x => x.QuestionType).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.PromptMarkdown).IsRequired();
        builder.Property(x => x.FunctionName).HasMaxLength(128);

        builder.HasIndex(x => new { x.AssessmentId, x.OrderIndex });

        builder.HasOne(x => x.Assessment)
            .WithMany()
            .HasForeignKey(x => x.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.TestCases)
            .WithOne(t => t.Question!)
            .HasForeignKey(t => t.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class AssessmentTestCaseConfiguration : IEntityTypeConfiguration<AssessmentTestCase>
{
    public void Configure(EntityTypeBuilder<AssessmentTestCase> builder)
    {
        builder.ToTable("AssessmentTestCases");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Input).IsRequired();
        builder.Property(x => x.ExpectedOutput).IsRequired();
        builder.HasIndex(x => new { x.QuestionId, x.OrderIndex });
    }
}

public sealed class AssessmentAttemptAnswerConfiguration
    : IEntityTypeConfiguration<AssessmentAttemptAnswer>
{
    public void Configure(EntityTypeBuilder<AssessmentAttemptAnswer> builder)
    {
        builder.ToTable("AssessmentAttemptAnswers");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.Language).HasMaxLength(32);

        // Draft saves upsert against this key.
        builder.HasIndex(x => new { x.AttemptId, x.QuestionId }).IsUnique();

        builder.HasOne(x => x.Attempt)
            .WithMany()
            .HasForeignKey(x => x.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Question)
            .WithMany()
            .HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
