using System.Text.Json;

namespace CareerPlatform.Api.Features.Assessments.Dto;

/// <summary>
/// Reads and writes the small JSON payload columns on a question (options, starter code).
///
/// Centralised because both the student projection and the admin projection need the same tolerant
/// behaviour: authored content that predates a schema tweak, or was hand-edited in the database,
/// must degrade to an empty collection rather than throwing a 500 in the middle of an exam.
/// </summary>
internal static class JsonPayload
{
    internal static IReadOnlyList<string> ReadStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return Array.Empty<string>();
        }
    }

    internal static IReadOnlyDictionary<string, string> ReadStringMap(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string>();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    /// <summary>Serialises a collection, or returns null when there is nothing worth storing.</summary>
    internal static string? Write<T>(T? value) where T : class =>
        value is null ? null : JsonSerializer.Serialize(value);
}
