using CareerPlatform.Api.Features.Resumes.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class ResumeSubmissionConfiguration : IEntityTypeConfiguration<ResumeSubmission>
{
    public void Configure(EntityTypeBuilder<ResumeSubmission> builder)
    {
        builder.ToTable("ResumeSubmissions");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.TemplateCode);
        builder.Ignore(s => s.DomainEvents);
    }
}
