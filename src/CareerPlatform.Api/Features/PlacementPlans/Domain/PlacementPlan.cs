using System.ComponentModel.DataAnnotations;
using CareerPlatform.Api.Common;

namespace CareerPlatform.Api.Features.PlacementPlans.Domain;

/// <summary>
/// A campus placement-preparation program (a curated, time-boxed prep track). Fully admin-managed
/// via the CRUD endpoints — the public placement pages carry no hardcoded programs.
/// </summary>
public sealed class PlacementPlan : AuditableEntity<int>
{
    [Required, MaxLength(160)]
    public string Slug { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Program length in weeks.</summary>
    public int DurationWeeks { get; set; }

    /// <summary>Enrolment price in INR (whole rupees). 0 = free.</summary>
    public decimal Price { get; set; }

    public bool IsPublished { get; set; } = true;
}
