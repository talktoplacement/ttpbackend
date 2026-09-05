using CareerPlatform.Api.Features.Practice.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class PracticeBookmarkConfiguration : IEntityTypeConfiguration<PracticeBookmark>
{
    public void Configure(EntityTypeBuilder<PracticeBookmark> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.PracticeQuestionId }).IsUnique();

        builder.HasOne(x => x.PracticeQuestion)
            .WithMany()
            .HasForeignKey(x => x.PracticeQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
