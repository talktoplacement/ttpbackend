namespace CareerPlatform.Api.Features.Content.Dto;

// ── Response DTOs ────────────────────────────────────────────────────────────

/// <summary>Outward-facing language projection (admin list + creation response).</summary>
public sealed record LanguageResponse(
    int Id,
    string Title,
    string Slug,
    string Description,
    bool IsPublished,
    decimal Price,
    DateTime LastUpdated);

/// <summary>Topic detail for the admin edit surface.</summary>
public sealed record TopicDetailResponse(
    int Id,
    int SectionId,
    string Title,
    string Slug,
    string Content,
    int OrderIndex,
    string SectionTitle,
    string LanguageSlug,
    bool IsPaid);

/// <summary>Public curriculum root — nested Language → Sections → Topics.</summary>
public sealed record CurriculumResponse(
    int Id,
    string Title,
    string Slug,
    string Description,
    IReadOnlyList<CurriculumSectionResponse> Sections);

public sealed record CurriculumSectionResponse(
    int Id,
    string Title,
    int OrderIndex,
    IReadOnlyList<CurriculumTopicResponse> Topics);

public sealed record CurriculumTopicResponse(
    int Id,
    string Title,
    string Slug,
    string Content,
    int OrderIndex,
    string? CompanyTags,
    string? Frequency,
    string? Difficulty,
    int? ReadTimeMinutes,
    DateTime? LastUpdatedUtc,
    /// <summary>Whether this topic is premium (author marked it Paid).</summary>
    bool IsPaid,
    /// <summary>
    /// True when the caller is NOT entitled to read a paid topic. The <see cref="Content"/> is
    /// withheld (empty) in that case; the client renders a locked state + upgrade CTA.
    /// </summary>
    bool IsLocked);

/// <summary>Per-row error surfaced from the Excel importer.</summary>
public sealed record ImportRowError(int Row, string Message);

/// <summary>Summary body returned by the interview-question import endpoint.</summary>
public sealed record ImportInterviewQuestionsResponse(
    int TotalRows,
    int LanguagesCreated,
    int SectionsCreated,
    int TopicsCreated,
    int TopicsUpdated,
    IReadOnlyList<ImportRowError> Errors);

// ── Request DTOs ─────────────────────────────────────────────────────────────

/// <summary>Body for <c>POST /api/ContentManagement/languages</c>.</summary>
public sealed record CreateLanguageRequest(
    string Title, string Slug, string Description = "", bool IsPublished = true, decimal Price = 0);

/// <summary>
/// Body for <c>PUT /api/v1/content-management/languages/{id}</c>. Partial update — only non-null
/// fields are applied, so the same admin form can edit any subset of a track's metadata.
/// </summary>
public sealed record UpdateLanguageRequest(
    string? Title, string? Slug, string? Description, bool? IsPublished, decimal? Price);

/// <summary>Body for <c>POST /api/ContentManagement/sections</c>.</summary>
public sealed record CreateSectionRequest(int LanguageId, string Title, int OrderIndex = 0);

/// <summary>Body for <c>POST /api/ContentManagement/topics</c>.</summary>
public sealed record CreateTopicRequest(
    int SectionId,
    string Title,
    string Slug,
    string Content,
    int OrderIndex = 0,
    string? CompanyTags = null,
    string? Frequency = null,
    string? Difficulty = null,
    int? ReadTimeMinutes = null,
    bool IsPaid = false);

/// <summary>Body for <c>PUT /api/ContentManagement/topics/{id}</c>.</summary>
public sealed record UpdateTopicRequest(
    string Title,
    string Slug,
    string Content,
    int OrderIndex = 0,
    string? CompanyTags = null,
    string? Frequency = null,
    string? Difficulty = null,
    int? ReadTimeMinutes = null,
    bool IsPaid = false);

/// <summary>Body for reorder endpoints — new display order as a list of ids.</summary>
public sealed record ReorderRequest(List<int> OrderedIds);

/// <summary>Body for <c>PUT /api/ContentManagement/languages/{id}/publish</c>.</summary>
public sealed record SetPublishedRequest(bool IsPublished);

/// <summary>Body for <c>PUT /api/ContentManagement/languages/{id}/price</c>.</summary>
public sealed record UpdateLanguagePriceRequest(decimal Price);
