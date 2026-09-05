using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.MentorshipPlans.Domain;

/// <summary>
/// A mentorship pricing tier / package (e.g. a single 45-minute 1:1 call). Fully admin-managed;
/// the public mentorship pages carry no hardcoded tiers.
/// </summary>
public sealed class MentorshipPlan : AuditableEntity<int>
{
    [Required, MaxLength(160)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Session length in minutes.</summary>
    public int DurationMinutes { get; set; }

    /// <summary>Mentor fee in INR (whole rupees).</summary>
    public decimal Price { get; set; }

    /// <summary>Platform commission percentage (0-100) taken from the mentor fee.</summary>
    public decimal CommissionPercent { get; set; }

    public bool IsPublished { get; set; } = true;
}
