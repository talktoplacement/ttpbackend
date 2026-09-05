using CareerPlatform.Api.Features.Resumes.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class ResumeDraftConfiguration : IEntityTypeConfiguration<ResumeDraft>
{
    public void Configure(EntityTypeBuilder<ResumeDraft> b)
    {
        b.ToTable("ResumeDrafts");
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);

        b.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.TemplateCode).IsRequired().HasMaxLength(64);
        // Deliberately unbounded at the column level; the service caps the payload at 64 KB so the
        // limit lives with the validation message that explains it.
        b.Property(x => x.ContentJson).IsRequired();

        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.UserId, x.LastEditedAtUtc });
    }
}
