using CareerPlatform.Api.Features.Support.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketMessageConfiguration : IEntityTypeConfiguration<SupportTicketMessage>
{
    public void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.AuthorUserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.AuthorRole).IsRequired().HasMaxLength(16);
        builder.Property(x => x.Body).IsRequired();

        builder.HasIndex(x => x.TicketId);
    }
}
