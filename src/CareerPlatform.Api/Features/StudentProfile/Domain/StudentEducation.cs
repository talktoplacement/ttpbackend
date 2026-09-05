using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.StudentProfile.Domain;

/// <summary>
/// One qualification claimed by a student on their profile.
///
/// The grade is a (value, scale) pair rather than a bare number: a 9.2 means nothing without knowing
/// whether the institution grades on a 10-point CGPA, a 4-point GPA, or a percentage. Storing the
/// scale keeps the rendering honest and lets a readiness calculation normalise across conventions.
/// </summary>
public sealed class StudentEducation : AuditableEntity<int>
{
    [Required, MaxLength(64)]
    public string UserId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Degree { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Institution { get; set; } = string.Empty;

    [MaxLength(160)]
    public string? FieldOfStudy { get; set; }

    public int StartYear { get; set; }

    /// <summary>Null while the student is still enrolled — paired with <see cref="IsCurrent"/>.</summary>
    public int? EndYear { get; set; }

    public bool IsCurrent { get; set; }

    /// <summary>Null when the student chooses not to disclose a grade.</summary>
    public decimal? GradeValue { get; set; }

    /// <summary>See <see cref="GradeScales"/>. Required whenever <see cref="GradeValue"/> is set.</summary>
    [MaxLength(16)]
    public string? GradeScale { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>
/// Supported grading conventions and their upper bounds.
///
/// Both the validator and the readiness calculation read from here, so the set of accepted scales and
/// the maximum used to normalise them can never disagree.
/// </summary>
public static class GradeScales
{
    public const string Cgpa10 = "cgpa-10";
    public const string Cgpa4 = "cgpa-4";
    public const string Percentage = "percentage";

    private static readonly Dictionary<string, decimal> Maximums =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [Cgpa10] = 10m,
            [Cgpa4] = 4m,
            [Percentage] = 100m,
        };

    public static IReadOnlyCollection<string> All => Maximums.Keys;

    public static bool IsSupported(string? scale) =>
        !string.IsNullOrWhiteSpace(scale) && Maximums.ContainsKey(scale);

    public static decimal? MaximumFor(string? scale) =>
        scale is not null && Maximums.TryGetValue(scale, out var max) ? max : null;

    /// <summary>Grade as a 0..1 fraction, or null when the scale is unknown or the value absent.</summary>
    public static decimal? Normalise(decimal? value, string? scale)
    {
        if (value is null) return null;
        var max = MaximumFor(scale);
        if (max is null || max == 0) return null;
        return Math.Clamp(value.Value / max.Value, 0m, 1m);
    }
}
