using CareerPlatform.Api.Features.Posts.Dto;

namespace CareerPlatform.Api.Features.Posts.Service;

public interface IPostService
{
    // ── Author self ──────────────────────────────────────────────────────────
    Task<Result<IReadOnlyList<PostSummaryResponse>>> ListMineAsync(CancellationToken ct);
    Task<Result<PostResponse>> GetMineAsync(int id, CancellationToken ct);
    Task<Result<PostResponse>> CreateAsync(PostEditorRequest request, CancellationToken ct);
    Task<Result<PostResponse>> UpdateAsync(int id, PostEditorRequest request, CancellationToken ct);
    Task<Result<PostResponse>> SubmitAsync(int id, CancellationToken ct);
    Task<Result> DeleteAsync(int id, CancellationToken ct);

    // ── Admin review ─────────────────────────────────────────────────────────
    Task<Result<IReadOnlyList<PostSummaryResponse>>> ListForReviewAsync(string? status, CancellationToken ct);
    Task<Result<PostResponse>> GetForReviewAsync(int id, CancellationToken ct);
    Task<Result<PostResponse>> ReviewAsync(int id, ReviewPostRequest request, CancellationToken ct);

    // ── Admin direct authoring (no review step) ────────────────────────────────
    Task<Result<PostResponse>> AdminCreateAsync(PostEditorRequest request, CancellationToken ct);
    Task<Result<PostResponse>> AdminUpdateAsync(int id, PostEditorRequest request, CancellationToken ct);
    Task<Result<PostResponse>> AdminPublishAsync(int id, CancellationToken ct);
    Task<Result<PostResponse>> AdminUnpublishAsync(int id, CancellationToken ct);
    Task<Result> AdminDeleteAsync(int id, CancellationToken ct);

    // ── Public ─────────────────────────────────────────────────────────────--
    Task<Result<IReadOnlyList<PostSummaryResponse>>> ListPublishedAsync(string? tag, CancellationToken ct);
    Task<Result<PostResponse>> GetPublishedBySlugAsync(string slug, CancellationToken ct);
}
