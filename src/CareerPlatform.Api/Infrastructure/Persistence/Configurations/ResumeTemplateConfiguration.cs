using CareerPlatform.Api.Features.Resumes.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class ResumeTemplateConfiguration : IEntityTypeConfiguration<ResumeTemplate>
{
    public void Configure(EntityTypeBuilder<ResumeTemplate> builder)
    {
        builder.ToTable("ResumeTemplates");
        builder.HasKey(t => t.Id);
        builder.HasIndex(t => t.Code).IsUnique();
        builder.HasIndex(t => t.IsPublished);
        builder.Ignore(t => t.DomainEvents);
    }
}
