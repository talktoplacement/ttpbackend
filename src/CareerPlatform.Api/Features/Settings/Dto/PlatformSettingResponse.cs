using CareerPlatform.Api.Features.Settings.Domain;

namespace CareerPlatform.Api.Features.Settings.Dto;

/// <summary>
/// Outward-facing projection of a <see cref="PlatformSetting"/>. Carries everything the frontend
/// needs to render and edit the control dynamically — label, help text, value type, current value,
/// and ordering — so settings pages contain no hardcoded fields. <see cref="ValueType"/> is
/// serialized as its string name so the client renders the right control.
/// </summary>
public sealed record PlatformSettingResponse(
    string Key,
    string Category,
    string Label,
    string Description,
    string ValueType,
    string Value,
    int DisplayOrder)
{
    public static PlatformSettingResponse From(PlatformSetting s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return new PlatformSettingResponse(
            s.Key, s.Category, s.Label, s.Description, s.ValueType.ToString(), s.Value, s.DisplayOrder);
    }
}
