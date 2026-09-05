using CareerPlatform.Api.Features.Notifications.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>
/// Reproduces the legacy <c>Notification</c> model config verbatim: a composite index on
/// <c>(UserId, IsRead)</c> for fast per-user unread reads (Req 18.1, 18.2, 24.5).
/// </summary>
public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.HasIndex(n => new { n.UserId, n.IsRead });

        builder.Ignore(n => n.DomainEvents);
    }
}
