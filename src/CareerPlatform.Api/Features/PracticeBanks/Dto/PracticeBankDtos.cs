using CareerPlatform.Api.Features.PracticeBanks.Domain;

namespace CareerPlatform.Api.Features.PracticeBanks.Dto;

/// <summary>Bank metadata plus a question count (no question payload — keeps list responses small).</summary>
public sealed record PracticeBankResponse(
    int Id, string Slug, string Name, string? Description,
    bool IsPublished, int QuestionCount)
{
    public static PracticeBankResponse From(PracticeQuestionBank b, int questionCount)
    {
        ArgumentNullException.ThrowIfNull(b);
        return new PracticeBankResponse(
            b.Id, b.Slug, b.Name, b.Description, b.IsPublished, questionCount);
    }
}

/// <summary>A question as it appears inside a bank (bank-scoped order + question summary).</summary>
public sealed record PracticeBankQuestionResponse(
    int QuestionId, string Slug, string Title,
    string Difficulty, string Category, int OrderIndex);

/// <summary>Bank detail: metadata + ordered membership.</summary>
public sealed record PracticeBankDetailResponse(
    int Id, string Slug, string Name, string? Description, bool IsPublished,
    IReadOnlyList<PracticeBankQuestionResponse> Questions);

public sealed record CreatePracticeBankRequest(
    string Slug, string Name, string? Description, bool IsPublished = true);

public sealed record UpdatePracticeBankRequest(
    string Name, string? Description, bool IsPublished);

/// <summary>
/// Full replacement of a bank's membership. Order in the array becomes the display order, so the
/// caller controls sequence without a separate reorder call.
/// </summary>
public sealed record SetBankQuestionsRequest(IReadOnlyList<int> QuestionIds);
