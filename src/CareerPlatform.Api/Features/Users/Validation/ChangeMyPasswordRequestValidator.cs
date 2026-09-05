using CareerPlatform.Api.Features.Users.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Users.Validation;

public sealed class ChangeMyPasswordRequestValidator : AbstractValidator<ChangeMyPasswordRequest>
{
    public ChangeMyPasswordRequestValidator()
    {
        RuleFor(c => c.CurrentPassword).NotEmpty().MaximumLength(200);
        RuleFor(c => c.NewPassword)
            .NotEmpty()
            .MinimumLength(8)
            .Matches(@"[A-Za-z]").WithMessage("New password must contain at least one letter.")
            .Matches(@"\d").WithMessage("New password must contain at least one digit.");
        RuleFor(c => c)
            .Must(c => c.CurrentPassword != c.NewPassword)
            .WithMessage("The new password must differ from the current password.");
    }
}
