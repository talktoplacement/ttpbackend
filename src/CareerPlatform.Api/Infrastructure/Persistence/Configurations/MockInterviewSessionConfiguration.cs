using CareerPlatform.Api.Features.Interviews.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class MockInterviewSessionConfiguration : IEntityTypeConfiguration<MockInterviewSession>
{
    public void Configure(EntityTypeBuilder<MockInterviewSession> builder)
    {
        builder.ToTable("MockInterviewSessions");
        builder.HasKey(s => s.Id);
        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.Status);
        builder.Ignore(s => s.DomainEvents);
    }
}
