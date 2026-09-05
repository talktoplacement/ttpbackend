using CareerPlatform.Api.Features.Payments.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Payments.Validation;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(c => c.PlanId).GreaterThan(0)
            .WithMessage("PlanId must be a positive plan identifier.");
    }
}
