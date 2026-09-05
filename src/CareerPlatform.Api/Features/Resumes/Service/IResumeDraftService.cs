using CareerPlatform.Api.Features.Resumes.Dto;

namespace CareerPlatform.Api.Features.Resumes.Service;

/// <summary>
/// Resume drafts for the authenticated caller.
///
/// Split from <see cref="IResumesService"/> — which already owns submissions, PDF uploads, templates,
/// ATS analysis and the admin/mentor surfaces — because drafts are an independent lifecycle with no
/// shared state. Adding four more methods to that interface would have made every consumer depend on
/// a wider contract than it uses.
/// </summary>
public interface IResumeDraftService
{
    Task<Result<IReadOnlyList<ResumeDraftResponse>>> ListMineAsync(CancellationToken ct);
    Task<Result<ResumeDraftResponse>> GetMineAsync(int id, CancellationToken ct);
    Task<Result<ResumeDraftResponse>> CreateMineAsync(CreateResumeDraftRequest request, CancellationToken ct);
    Task<Result<ResumeDraftResponse>> UpdateMineAsync(int id, UpdateResumeDraftRequest request, CancellationToken ct);
    Task<Result> DeleteMineAsync(int id, CancellationToken ct);
}
