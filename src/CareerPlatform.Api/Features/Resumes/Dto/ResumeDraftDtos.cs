using System.Text.Json;
using CareerPlatform.Api.Features.Resumes.Domain;

namespace CareerPlatform.Api.Features.Resumes.Dto;

/// <summary>
/// A resume draft. <see cref="Content"/> is emitted as a JSON object rather than an escaped string so
/// the client consumes it directly instead of double-parsing.
/// </summary>
public sealed record ResumeDraftResponse(
    int Id,
    string Title,
    string TemplateCode,
    JsonElement Content,
    DateTime LastEditedAt,
    DateTime CreatedAt)
{
    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();

    public static ResumeDraftResponse From(ResumeDraft d)
    {
        ArgumentNullException.ThrowIfNull(d);

        JsonElement content = EmptyObject;
        if (!string.IsNullOrWhiteSpace(d.ContentJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(d.ContentJson);
                content = doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                // A row written before validation tightened could hold malformed JSON. Degrade to an
                // empty document so one bad draft cannot break the whole list response.
                content = EmptyObject;
            }
        }

        return new ResumeDraftResponse(
            d.Id, d.Title, d.TemplateCode, content, d.LastEditedAtUtc, d.CreatedAtUtc);
    }
}

/// <summary>Body for creating a draft. Content is optional so a draft can be started empty.</summary>
public sealed record CreateResumeDraftRequest(string Title, string TemplateCode, JsonElement? Content);

/// <summary>
/// Body for saving a draft. Every field is optional so an autosave can send only what changed,
/// but at least one must be present — enforced by the validator.
/// </summary>
public sealed record UpdateResumeDraftRequest(string? Title, string? TemplateCode, JsonElement? Content);
