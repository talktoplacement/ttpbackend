using CareerPlatform.Api.Features.Auth.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Auth.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(r => r.Password).NotEmpty().MaximumLength(200);
    }
}

public sealed class StartRegistrationRequestValidator : AbstractValidator<StartRegistrationRequest>
{
    private static readonly string[] AllowedRoles = { "Student", "Mentor" };
    public StartRegistrationRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(r => r.FullName).NotEmpty().MaximumLength(200);
        RuleFor(r => r.MobileNumber).NotEmpty().MaximumLength(32);
        RuleFor(r => r.YearsOfExperience).MaximumLength(120);
        RuleFor(r => r.IntendedRole).NotEmpty()
            .Must(role => AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Intended role must be Student or Mentor.");
        RuleFor(r => r.Password).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.");
    }
}

public sealed class VerifyRegistrationRequestValidator : AbstractValidator<VerifyRegistrationRequest>
{
    public VerifyRegistrationRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(r => r.Code).NotEmpty().MaximumLength(16);
    }
}

public sealed class ResendOtpRequestValidator : AbstractValidator<ResendOtpRequest>
{
    public ResendOtpRequestValidator() =>
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(320);
}

public sealed class RequestPasswordResetRequestValidator : AbstractValidator<RequestPasswordResetRequest>
{
    public RequestPasswordResetRequestValidator() =>
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(320);
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(r => r.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(r => r.Code).NotEmpty().MaximumLength(16);
        RuleFor(r => r.NewPassword).NotEmpty().MinimumLength(8)
            .Matches(@"[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.");
    }
}
