using CareerPlatform.Api.Features.Support.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Category).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Priority).IsRequired().HasMaxLength(16);
        builder.Property(x => x.AssignedToUserId).HasMaxLength(64);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Messages)
            .WithOne(m => m.Ticket)
            .HasForeignKey(m => m.TicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
