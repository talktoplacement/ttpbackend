using CareerPlatform.Api.Features.PlacementCompanies.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.PlacementCompanies.Validation;

public sealed class CreatePlacementCompanyRequestValidator : AbstractValidator<CreatePlacementCompanyRequest>
{
    private static readonly string[] AllowedTiers =
        { "Tier 1", "Product Based", "Service Based", "High Growth Startup" };
    public CreatePlacementCompanyRequestValidator()
    {
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be kebab-case.");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Logo).MaximumLength(500);
        RuleFor(c => c.Tier).NotEmpty().Must(t => AllowedTiers.Contains(t))
            .WithMessage($"Tier must be one of: {string.Join(", ", AllowedTiers)}.");
        RuleFor(c => c.CtcRange).MaximumLength(64);
        RuleFor(c => c.OpenPositions).InclusiveBetween(0, 10_000);
        RuleFor(c => c.Description).MaximumLength(4000);
    }
}

public sealed class UpdatePlacementCompanyRequestValidator : AbstractValidator<UpdatePlacementCompanyRequest>
{
    private static readonly string[] AllowedTiers =
        { "Tier 1", "Product Based", "Service Based", "High Growth Startup" };
    public UpdatePlacementCompanyRequestValidator()
    {
        RuleFor(c => c.Slug).MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(c => !string.IsNullOrWhiteSpace(c.Slug))
            .WithMessage("Slug must be kebab-case.");
        RuleFor(c => c.Name).MaximumLength(200);
        RuleFor(c => c.Logo).MaximumLength(500);
        RuleFor(c => c.Tier).Must(t => t is null || AllowedTiers.Contains(t))
            .WithMessage($"Tier must be one of: {string.Join(", ", AllowedTiers)}.");
        RuleFor(c => c.CtcRange).MaximumLength(64);
        RuleFor(c => c.OpenPositions).InclusiveBetween(0, 10_000).When(c => c.OpenPositions.HasValue);
        RuleFor(c => c.Description).MaximumLength(4000);
    }
}
