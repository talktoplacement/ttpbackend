using CareerPlatform.Api.Features.PracticeBanks.Dto;

namespace CareerPlatform.Api.Features.PracticeBanks.Service;

public interface IPracticeBankService
{
    /// <summary>Public: published banks with question counts.</summary>
    Task<Result<IReadOnlyList<PracticeBankResponse>>> ListPublishedAsync(CancellationToken ct);

    /// <summary>Public: a single bank with its ordered question list.</summary>
    Task<Result<PracticeBankDetailResponse>> GetBySlugAsync(string slug, CancellationToken ct);

    /// <summary>Admin: every bank (published or not) with question counts.</summary>
    Task<Result<IReadOnlyList<PracticeBankResponse>>> ListAllAsync(CancellationToken ct);

    /// <summary>Admin: fetch by id (includes unpublished, unlike the public slug read).</summary>
    Task<Result<PracticeBankResponse>> GetByIdAsync(int id, CancellationToken ct);

    Task<Result<PracticeBankResponse>> CreateAsync(CreatePracticeBankRequest request, CancellationToken ct);
    Task<Result<PracticeBankResponse>> UpdateAsync(int id, UpdatePracticeBankRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);

    /// <summary>Admin: replace a bank's question membership (array order = display order).</summary>
    Task<Result<PracticeBankDetailResponse>> SetQuestionsAsync(
        int bankId, SetBankQuestionsRequest request, CancellationToken ct);
}
