using CareerPlatform.Api.Features.Payments.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Payments.Validation;

public sealed class VerifyPaymentRequestValidator : AbstractValidator<VerifyPaymentRequest>
{
    public VerifyPaymentRequestValidator()
    {
        // No PlanId rule: the plan is resolved from the stored Order, never from the request.
        RuleFor(c => c.RazorpayOrderId).NotEmpty().MaximumLength(128);
        RuleFor(c => c.RazorpayPaymentId).NotEmpty().MaximumLength(128);
        RuleFor(c => c.RazorpaySignature).NotEmpty().MaximumLength(256);
    }
}
