using CareerPlatform.Api.Features.Offers.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Offers.Validation;

/// <summary>
/// Validates <see cref="VerifyOfferRequest"/>: <c>Status</c> must be one of the two allowed
/// verification states. An invalid value short-circuits to a 400 ProblemDetails before the
/// service runs.
/// </summary>
public sealed class VerifyOfferRequestValidator : AbstractValidator<VerifyOfferRequest>
{
    public VerifyOfferRequestValidator()
    {
        RuleFor(r => r.Status)
            .NotEmpty()
            .Must(status => status is "Verified" or "Rejected")
            .WithMessage("Status must be 'Verified' or 'Rejected'.");
    }
}
