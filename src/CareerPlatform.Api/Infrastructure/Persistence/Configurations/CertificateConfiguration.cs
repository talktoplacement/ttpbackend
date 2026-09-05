using CareerPlatform.Api.Features.Certificates.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

/// <summary>EF Core model for <see cref="Certificate"/>. Matches the DDL in schema.sql.</summary>
public sealed class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Ignore(x => x.DomainEvents);

        builder.Property(x => x.UserId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(200);
        builder.Property(x => x.IssuedFor).HasMaxLength(200);
        builder.Property(x => x.VerificationCode).IsRequired().HasMaxLength(64);
        builder.Property(x => x.StorageKey).HasMaxLength(500);

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.VerificationCode).IsUnique();
    }
}
