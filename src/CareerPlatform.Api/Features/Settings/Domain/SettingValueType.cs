namespace CareerPlatform.Api.Features.Settings.Domain;

/// <summary>
/// The data type a <see cref="PlatformSetting"/>'s string value represents. Drives both server-side
/// validation (a Boolean must parse as true/false, a Number must parse as a decimal) and the
/// frontend control the setting renders as (text input, numeric input, or a toggle). Persisted as
/// an int so adding a new type is a data-compatible, additive change.
/// </summary>
public enum SettingValueType
{
    String = 0,
    Boolean = 1,
    Number = 2,
}
