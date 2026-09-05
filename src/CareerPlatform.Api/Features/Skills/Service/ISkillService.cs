using CareerPlatform.Api.Features.Skills.Dto;

namespace CareerPlatform.Api.Features.Skills.Service;

public interface ISkillService
{
    Task<Result<SkillsResponse>> GetMySkillsAsync(CancellationToken ct);
    Task<Result<SkillsResponse>> ReplaceMySkillsAsync(ReplaceSkillsRequest request, CancellationToken ct);
}
