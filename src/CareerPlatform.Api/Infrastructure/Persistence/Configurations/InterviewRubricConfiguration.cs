using CareerPlatform.Api.Features.Interviews.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class InterviewRubricConfiguration : IEntityTypeConfiguration<InterviewRubric>
{
    public void Configure(EntityTypeBuilder<InterviewRubric> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.HasIndex(x => x.DisplayOrder);
    }
}
