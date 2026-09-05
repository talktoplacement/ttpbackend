using CareerPlatform.Api.Features.MentorshipPlans.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.MentorshipPlans.Validation;

public sealed class CreateMentorshipPlanRequestValidator : AbstractValidator<CreateMentorshipPlanRequest>
{
    public CreateMentorshipPlanRequestValidator()
    {
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be kebab-case.");
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.DurationMinutes).InclusiveBetween(1, 1440);
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CommissionPercent).InclusiveBetween(0, 100);
    }
}

public sealed class UpdateMentorshipPlanRequestValidator : AbstractValidator<UpdateMentorshipPlanRequest>
{
    public UpdateMentorshipPlanRequestValidator()
    {
        RuleFor(c => c.Slug).MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(c => !string.IsNullOrWhiteSpace(c.Slug))
            .WithMessage("Slug must be kebab-case.");
        RuleFor(c => c.Title).MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.DurationMinutes).InclusiveBetween(1, 1440).When(c => c.DurationMinutes.HasValue);
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0).When(c => c.Price.HasValue);
        RuleFor(c => c.CommissionPercent).InclusiveBetween(0, 100).When(c => c.CommissionPercent.HasValue);
    }
}
