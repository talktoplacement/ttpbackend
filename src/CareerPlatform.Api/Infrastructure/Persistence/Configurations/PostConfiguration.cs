using CareerPlatform.Api.Features.Posts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.AuthorUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Slug).IsRequired().HasMaxLength(220);
        builder.Property(x => x.ContentMarkdown).IsRequired();
        builder.Property(x => x.Excerpt).HasMaxLength(500);
        builder.Property(x => x.CoverImageUrl).HasMaxLength(500);
        builder.Property(x => x.Tags).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(16);
        builder.Property(x => x.ReviewedByUserId).HasMaxLength(64);
        builder.Property(x => x.ReviewNote).HasMaxLength(2000);

        builder.HasIndex(x => x.AuthorUserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Slug).IsUnique();
    }
}
