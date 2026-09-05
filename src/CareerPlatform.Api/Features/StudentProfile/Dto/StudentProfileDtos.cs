using CareerPlatform.Api.Features.StudentProfile.Domain;

namespace CareerPlatform.Api.Features.StudentProfile.Dto;

// ── Education ───────────────────────────────────────────────────────────────────

public sealed record EducationResponse(
    int Id,
    string Degree,
    string Institution,
    string? FieldOfStudy,
    int StartYear,
    int? EndYear,
    bool IsCurrent,
    decimal? GradeValue,
    string? GradeScale,
    /// <summary>Upper bound of <paramref name="GradeScale"/>, so the client renders "9.2 / 10"
    /// without hardcoding a denominator.</summary>
    decimal? GradeMaximum,
    string? Description,
    int DisplayOrder)
{
    public static EducationResponse From(StudentEducation e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new EducationResponse(
            e.Id, e.Degree, e.Institution, e.FieldOfStudy, e.StartYear, e.EndYear, e.IsCurrent,
            e.GradeValue, e.GradeScale, GradeScales.MaximumFor(e.GradeScale),
            e.Description, e.DisplayOrder);
    }
}

/// <summary>The caller's qualifications plus the grading scales the API accepts.</summary>
public sealed record EducationListResponse(
    IReadOnlyList<EducationResponse> Items,
    IReadOnlyList<string> SupportedGradeScales);

public sealed record UpsertEducationRequest(
    string Degree,
    string Institution,
    string? FieldOfStudy,
    int StartYear,
    int? EndYear,
    bool IsCurrent,
    decimal? GradeValue,
    string? GradeScale,
    string? Description,
    int DisplayOrder);

// ── Preferences ─────────────────────────────────────────────────────────────────

public sealed record PreferencesResponse(
    bool EmailNotificationsEnabled,
    bool RecruiterVisibility,
    bool MentorshipRemindersEnabled,
    bool PromotionalEmailsEnabled,
    string? PreferredRole,
    IReadOnlyList<string> PreferredLocations)
{
    public static PreferencesResponse From(StudentPreferences p)
    {
        ArgumentNullException.ThrowIfNull(p);
        return new PreferencesResponse(
            p.EmailNotificationsEnabled,
            p.RecruiterVisibility,
            p.MentorshipRemindersEnabled,
            p.PromotionalEmailsEnabled,
            p.PreferredRole,
            SplitLocations(p.PreferredLocations));
    }

    private static IReadOnlyList<string> SplitLocations(string? raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<string>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}

/// <summary>
/// Full replacement of the caller's preferences. Every switch is required so a partial payload can
/// never silently leave a consent flag at its previous value.
/// </summary>
public sealed record UpdatePreferencesRequest(
    bool EmailNotificationsEnabled,
    bool RecruiterVisibility,
    bool MentorshipRemindersEnabled,
    bool PromotionalEmailsEnabled,
    string? PreferredRole,
    IReadOnlyList<string>? PreferredLocations);
