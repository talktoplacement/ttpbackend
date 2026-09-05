using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.PracticeBanks.Domain;

/// <summary>
/// A curated grouping of practice questions (e.g. "Amazon SDE-1 Top 75"). Membership is stored in
/// <see cref="PracticeQuestionBankItem"/> so the same question can appear in many banks with a
/// per-bank display order.
/// </summary>
public sealed class PracticeQuestionBank : AuditableEntity<int>
{
    [Required, MaxLength(64)] public string Slug { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Description { get; set; }
    public bool IsPublished { get; set; } = true;
}

/// <summary>
/// Join row for the bank↔question many-to-many. Carries a surrogate <c>Id</c> (so it satisfies the
/// project-wide <see cref="Entity{TId}"/> convention) plus a UNIQUE index on
/// <c>(BankId, QuestionId)</c> which is what actually prevents the same question being added to a
/// bank twice. <see cref="OrderIndex"/> controls sequence within the bank.
/// </summary>
public sealed class PracticeQuestionBankItem : Entity<int>
{
    public int BankId { get; set; }
    public int QuestionId { get; set; }
    public int OrderIndex { get; set; }
}
