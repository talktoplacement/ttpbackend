using System.Globalization;
using CareerPlatform.Api.Features.Settings.Domain;
using CareerPlatform.Api.Features.Settings.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Settings.Service;

/// <summary>
/// Platform-settings admin service. Ports the two legacy MediatR handlers verbatim. Unknown keys
/// are rejected before any write; each incoming value is coerced + validated against its target
/// setting's <c>ValueType</c> (Boolean → normalized "true"/"false"; Number → invariant decimal).
/// </summary>
internal sealed class SettingsService : ISettingsService
{
    private readonly AppDbContext _db;
    public SettingsService(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<PlatformSettingResponse>>> ListAsync(
        string? category, CancellationToken ct)
    {
        var query = _db.PlatformSettings.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(category))
        {
            var c = category.Trim();
            query = query.Where(s => s.Category == c);
        }
        var settings = await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);

        IReadOnlyList<PlatformSettingResponse> items =
            settings.Select(PlatformSettingResponse.From).ToList();
        return Result.Success(items);
    }

    public async Task<Result<IReadOnlyList<PlatformSettingResponse>>> UpdateAsync(
        IReadOnlyList<SettingUpdate> updates, CancellationToken ct)
    {
        var keys = updates.Select(u => u.Key.Trim()).ToList();
        var settings = await _db.PlatformSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync(ct);
        var byKey = settings.ToDictionary(s => s.Key, StringComparer.Ordinal);

        var unknown = keys.Where(k => !byKey.ContainsKey(k)).Distinct().ToList();
        if (unknown.Count > 0)
        {
            return Result.Failure<IReadOnlyList<PlatformSettingResponse>>(Error.Validation(
                "Settings.UnknownKey",
                $"Unknown setting key(s): {string.Join(", ", unknown)}."));
        }

        foreach (var update in updates)
        {
            var setting = byKey[update.Key.Trim()];
            var (ok, normalized, error) = Coerce(setting, update.Value);
            if (!ok)
            {
                return Result.Failure<IReadOnlyList<PlatformSettingResponse>>(Error.Validation(
                    "Settings.InvalidValue", error!));
            }
            setting.Value = normalized!;
        }

        await _db.SaveChangesAsync(ct);

        var all = await _db.PlatformSettings.AsNoTracking()
            .OrderBy(s => s.Category)
            .ThenBy(s => s.DisplayOrder)
            .ThenBy(s => s.Id)
            .ToListAsync(ct);
        IReadOnlyList<PlatformSettingResponse> items =
            all.Select(PlatformSettingResponse.From).ToList();
        return Result.Success(items);
    }

    private static (bool Ok, string? Normalized, string? Error) Coerce(
        PlatformSetting setting, string rawValue)
    {
        var value = rawValue?.Trim() ?? string.Empty;
        switch (setting.ValueType)
        {
            case SettingValueType.Boolean:
                if (bool.TryParse(value, out var b))
                {
                    return (true, b ? "true" : "false", null);
                }
                return (false, null, $"'{setting.Label}' must be true or false.");
            case SettingValueType.Number:
                if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var n))
                {
                    return (true, n.ToString(CultureInfo.InvariantCulture), null);
                }
                return (false, null, $"'{setting.Label}' must be a valid number.");
            case SettingValueType.String:
            default:
                return (true, value, null);
        }
    }
}
