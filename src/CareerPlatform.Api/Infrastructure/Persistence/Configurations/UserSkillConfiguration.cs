using CareerPlatform.Api.Features.Skills.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class UserSkillConfiguration : IEntityTypeConfiguration<UserSkill>
{
    public void Configure(EntityTypeBuilder<UserSkill> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        b.Property(x => x.SkillName).IsRequired().HasMaxLength(100);
        b.Property(x => x.Category).IsRequired().HasMaxLength(64);
        b.Property(x => x.ProficiencyLevel).IsRequired().HasMaxLength(16);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => new { x.UserId, x.SkillName }).IsUnique();
    }
}
