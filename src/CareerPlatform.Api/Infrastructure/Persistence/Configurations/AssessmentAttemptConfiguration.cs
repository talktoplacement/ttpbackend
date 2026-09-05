using CareerPlatform.Api.Features.Assessments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class AssessmentAttemptConfiguration : IEntityTypeConfiguration<AssessmentAttempt>
{
    public void Configure(EntityTypeBuilder<AssessmentAttempt> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.AnswersJson).IsRequired();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.AssessmentId);
        builder.HasIndex(x => new { x.UserId, x.AssessmentId });

        builder.HasOne(x => x.Assessment)
            .WithMany()
            .HasForeignKey(x => x.AssessmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
