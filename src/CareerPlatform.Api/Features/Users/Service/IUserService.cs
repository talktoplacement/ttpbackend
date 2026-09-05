using CareerPlatform.Api.Features.Users.Dto;

namespace CareerPlatform.Api.Features.Users.Service;

public interface IUserService
{
    Task<Result<MyProfileResponse>> GetMineAsync(CancellationToken ct);
    Task<Result<MyProfileResponse>> UpdateMineAsync(UpdateMyProfileRequest request, CancellationToken ct);
    Task<Result> ChangeMyPasswordAsync(string currentPassword, string newPassword, CancellationToken ct);
    Task<Result<MyProfileResponse>> SyncAsync(string? displayName, CancellationToken ct);
}
