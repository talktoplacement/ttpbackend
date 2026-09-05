using CareerPlatform.Api.Features.Students.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>ProgressLog</c> model config: int PK by convention; the
/// ProgressLog→UserProfile and ProgressLog→CourseContent relationships are discovered by
/// annotation as in the legacy model (Req 18.1, 18.2).
/// </summary>
public sealed class ProgressLogConfiguration : IEntityTypeConfiguration<ProgressLog>
{
    public void Configure(EntityTypeBuilder<ProgressLog> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Ignore(p => p.DomainEvents);
    }
}
