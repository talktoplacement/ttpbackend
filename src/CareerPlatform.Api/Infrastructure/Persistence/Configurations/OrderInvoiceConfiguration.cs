using CareerPlatform.Api.Features.AdminLedger.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class OrderInvoiceConfiguration : IEntityTypeConfiguration<OrderInvoice>
{
    public void Configure(EntityTypeBuilder<OrderInvoice> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.OrderId).IsRequired().HasMaxLength(64);
        b.Property(x => x.CustomerUserId).IsRequired().HasMaxLength(64);
        b.Property(x => x.CustomerEmail).HasMaxLength(320);
        b.Property(x => x.ItemDescription).IsRequired().HasMaxLength(500);
        b.Property(x => x.Currency).IsRequired().HasMaxLength(8);
        b.Property(x => x.Status).IsRequired().HasMaxLength(16);
        b.Property(x => x.Amount).HasColumnType("numeric(12,2)");
        b.HasIndex(x => x.OrderId).IsUnique();
        b.HasIndex(x => x.CustomerUserId);
    }
}

public sealed class AdminAuditLogConfiguration : IEntityTypeConfiguration<AdminAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminAuditLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.ActorUserId).IsRequired().HasMaxLength(64);
        b.Property(x => x.ActorEmail).HasMaxLength(320);
        b.Property(x => x.Action).IsRequired().HasMaxLength(64);
        b.Property(x => x.TargetKind).HasMaxLength(64);
        b.Property(x => x.TargetId).HasMaxLength(128);
        b.Property(x => x.Metadata).HasColumnType("jsonb");
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.HasIndex(x => x.OccurredAtUtc).IsDescending();
        b.HasIndex(x => x.ActorUserId);
    }
}
