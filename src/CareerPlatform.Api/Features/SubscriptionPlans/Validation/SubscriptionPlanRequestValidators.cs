using CareerPlatform.Api.Features.SubscriptionPlans.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.SubscriptionPlans.Validation;

public sealed class CreatePlanRequestValidator : AbstractValidator<CreatePlanRequest>
{
    public CreatePlanRequestValidator()
    {
        RuleFor(c => c.Code).NotEmpty().MaximumLength(64)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Code must be kebab-case.");
        RuleFor(c => c.Name).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0);
        RuleFor(c => c.Currency).MaximumLength(3).When(c => !string.IsNullOrWhiteSpace(c.Currency));
        RuleFor(c => c.IntervalCount).GreaterThan(0);
    }
}

public sealed class UpdatePlanRequestValidator : AbstractValidator<UpdatePlanRequest>
{
    public UpdatePlanRequestValidator()
    {
        RuleFor(c => c.Code).MaximumLength(64)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(c => !string.IsNullOrWhiteSpace(c.Code));
        RuleFor(c => c.Name).MaximumLength(120);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0).When(c => c.Price.HasValue);
        RuleFor(c => c.Currency).MaximumLength(3).When(c => !string.IsNullOrWhiteSpace(c.Currency));
        RuleFor(c => c.IntervalCount).GreaterThan(0).When(c => c.IntervalCount.HasValue);
    }
}
