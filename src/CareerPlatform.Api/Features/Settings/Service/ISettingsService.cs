using CareerPlatform.Api.Features.Settings.Dto;

namespace CareerPlatform.Api.Features.Settings.Service;

public interface ISettingsService
{
    Task<Result<IReadOnlyList<PlatformSettingResponse>>> ListAsync(string? category, CancellationToken ct);
    Task<Result<IReadOnlyList<PlatformSettingResponse>>> UpdateAsync(
        IReadOnlyList<SettingUpdate> updates, CancellationToken ct);
}
