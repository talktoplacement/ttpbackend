using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.Settings.Domain;

/// <summary>
/// A single admin-managed platform configuration value, stored as a typed key/value row. The table
/// is the single source of truth for every operational setting surfaced on the admin settings
/// pages (general / notifications / payment / security), so there are no hardcoded config values in
/// the frontend — each settings screen renders whatever rows the database returns for its category.
///
/// Adding a new setting is a data-only operation (insert a row via the seeder or SQL): no code
/// change is required for it to appear and persist. Secret credentials (e.g. gateway secret keys)
/// are intentionally NOT stored here — those remain in server-only environment configuration.
/// </summary>
public sealed class PlatformSetting : AuditableEntity<int>
{
    /// <summary>Stable, unique machine key, e.g. <c>"general.platformName"</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Grouping used by the settings UI, e.g. <c>"general"</c>, <c>"payment"</c>.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Human-readable label rendered next to the control.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Optional helper text shown under the control.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>The type the <see cref="Value"/> string represents (drives validation + UI control).</summary>
    public SettingValueType ValueType { get; set; } = SettingValueType.String;

    /// <summary>The current value, stored as a string and interpreted per <see cref="ValueType"/>.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Ordering hint so the UI can render controls in a stable, intentional sequence.</summary>
    public int DisplayOrder { get; set; }
}
