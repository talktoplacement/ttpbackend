using CareerPlatform.Api.Features.StudentProfile.Domain;
using CareerPlatform.Api.Features.StudentProfile.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.StudentProfile.Validation;

public sealed class UpsertEducationRequestValidator : AbstractValidator<UpsertEducationRequest>
{
    /// <summary>
    /// Earliest plausible start year. A fixed floor beats "now minus N" because it does not move
    /// under the user, and beats no floor at all because a typo'd year would otherwise persist.
    /// </summary>
    private const int EarliestStartYear = 1950;

    /// <summary>
    /// Years a student may look ahead. Enrolment records legitimately carry a future end year, so the
    /// ceiling is the current year plus this allowance rather than the current year.
    /// </summary>
    private const int FutureYearAllowance = 10;

    public UpsertEducationRequestValidator()
    {
        var latestYear = DateTime.UtcNow.Year + FutureYearAllowance;

        RuleFor(e => e.Degree).NotEmpty().MaximumLength(200);
        RuleFor(e => e.Institution).NotEmpty().MaximumLength(200);
        RuleFor(e => e.FieldOfStudy).MaximumLength(160);
        RuleFor(e => e.Description).MaximumLength(1000);

        RuleFor(e => e.StartYear).InclusiveBetween(EarliestStartYear, latestYear);
        RuleFor(e => e.EndYear).InclusiveBetween(EarliestStartYear, latestYear)
            .When(e => e.EndYear.HasValue);

        RuleFor(e => e.GradeScale)
            .Must(GradeScales.IsSupported)
            .WithMessage($"GradeScale must be one of: {string.Join(", ", GradeScales.All)}.")
            .When(e => e.GradeValue.HasValue);

        RuleFor(e => e.GradeValue).GreaterThanOrEqualTo(0).When(e => e.GradeValue.HasValue);
        RuleFor(e => e.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdatePreferencesRequestValidator : AbstractValidator<UpdatePreferencesRequest>
{
    private const int MaxPreferredLocations = 10;

    public UpdatePreferencesRequestValidator()
    {
        RuleFor(p => p.PreferredRole).MaximumLength(160);

        RuleFor(p => p.PreferredLocations!)
            .Must(l => l.Count <= MaxPreferredLocations)
            .WithMessage($"At most {MaxPreferredLocations} preferred locations may be set.")
            .When(p => p.PreferredLocations is not null);

        RuleForEach(p => p.PreferredLocations!)
            .MaximumLength(80)
            .When(p => p.PreferredLocations is not null);
    }
}
