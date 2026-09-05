using CareerPlatform.Api.Features.Users.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Users.Validation;

public sealed class UpdateMyProfileRequestValidator : AbstractValidator<UpdateMyProfileRequest>
{
    public UpdateMyProfileRequestValidator()
    {
        RuleFor(r => r.FullName).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Phone).MaximumLength(32).When(r => !string.IsNullOrWhiteSpace(r.Phone));
        RuleFor(r => r.Designation).MaximumLength(120).When(r => !string.IsNullOrWhiteSpace(r.Designation));
        RuleFor(r => r.Department).MaximumLength(120).When(r => !string.IsNullOrWhiteSpace(r.Department));
    }
}
