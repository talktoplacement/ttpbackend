using CareerPlatform.Api.Features.PracticeBanks.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CareerPlatform.Api.Infrastructure.Persistence.Configurations;

public sealed class PracticeQuestionBankConfiguration
    : IEntityTypeConfiguration<PracticeQuestionBank>
{
    public void Configure(EntityTypeBuilder<PracticeQuestionBank> b)
    {
        b.HasKey(x => x.Id);
        b.Ignore(x => x.DomainEvents);
        b.Property(x => x.Slug).IsRequired().HasMaxLength(64);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.HasIndex(x => x.Slug).IsUnique();
    }
}

public sealed class PracticeQuestionBankItemConfiguration
    : IEntityTypeConfiguration<PracticeQuestionBankItem>
{
    public void Configure(EntityTypeBuilder<PracticeQuestionBankItem> b)
    {
        b.HasKey(x => x.Id);
        // The UNIQUE composite index — not the PK — is what enforces "a question appears at most
        // once per bank" at the DB level.
        b.HasIndex(x => new { x.BankId, x.QuestionId }).IsUnique();
        b.HasIndex(x => new { x.BankId, x.OrderIndex });
    }
}
