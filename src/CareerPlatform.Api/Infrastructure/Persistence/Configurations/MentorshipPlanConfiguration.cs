using CareerPlatform.Api.Features.MentorshipPlans.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class MentorshipPlanConfiguration : IEntityTypeConfiguration<MentorshipPlan>
{
    public void Configure(EntityTypeBuilder<MentorshipPlan> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Slug).IsRequired().HasMaxLength(160);
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.Price).HasColumnType("numeric(12,2)");
        b.Property(x => x.CommissionPercent).HasColumnType("numeric(5,2)");
        b.HasIndex(x => x.Slug).IsUnique();
    }
}
