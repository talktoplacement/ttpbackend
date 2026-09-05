using CareerPlatform.Api.Features.PlacementPlans.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.PlacementPlans.Validation;

public sealed class CreatePlacementPlanRequestValidator : AbstractValidator<CreatePlacementPlanRequest>
{
    public CreatePlacementPlanRequestValidator()
    {
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be kebab-case.");
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(4000);
        RuleFor(c => c.DurationWeeks).InclusiveBetween(0, 260);
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdatePlacementPlanRequestValidator : AbstractValidator<UpdatePlacementPlanRequest>
{
    public UpdatePlacementPlanRequestValidator()
    {
        RuleFor(c => c.Slug).MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(c => !string.IsNullOrWhiteSpace(c.Slug))
            .WithMessage("Slug must be kebab-case.");
        RuleFor(c => c.Title).MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(4000);
        RuleFor(c => c.DurationWeeks).InclusiveBetween(0, 260).When(c => c.DurationWeeks.HasValue);
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0).When(c => c.Price.HasValue);
    }
}
