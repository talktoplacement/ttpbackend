namespace CareerPlatform.Api.Features.Settings.Dto;

/// <summary>Body for <c>PUT /api/v1/admin/settings</c>: a batch of key/value updates to persist.</summary>
public sealed record UpdateSettingsRequest(IReadOnlyList<SettingUpdate> Updates);

/// <summary>A single key/value pair inside <see cref="UpdateSettingsRequest"/>.</summary>
public sealed record SettingUpdate(string Key, string Value);
