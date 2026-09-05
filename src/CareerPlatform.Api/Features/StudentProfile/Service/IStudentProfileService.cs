using CareerPlatform.Api.Features.StudentProfile.Dto;

namespace CareerPlatform.Api.Features.StudentProfile.Service;

/// <summary>
/// Student self-service profile data: qualifications and preferences. Every method is scoped to the
/// authenticated caller — there is deliberately no user-id parameter, so no route can be tricked
/// into reading or writing another student's profile.
/// </summary>
public interface IStudentProfileService
{
    Task<Result<EducationListResponse>> ListMyEducationAsync(CancellationToken ct);
    Task<Result<EducationResponse>> AddMyEducationAsync(UpsertEducationRequest request, CancellationToken ct);
    Task<Result<EducationResponse>> UpdateMyEducationAsync(int id, UpsertEducationRequest request, CancellationToken ct);
    Task<Result> DeleteMyEducationAsync(int id, CancellationToken ct);

    Task<Result<PreferencesResponse>> GetMyPreferencesAsync(CancellationToken ct);
    Task<Result<PreferencesResponse>> UpdateMyPreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct);
}
