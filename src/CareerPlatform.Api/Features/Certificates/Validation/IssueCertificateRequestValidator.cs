using CareerPlatform.Api.Features.Certificates.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Certificates.Validation;

public sealed class IssueCertificateRequestValidator : AbstractValidator<IssueCertificateRequest>
{
    public IssueCertificateRequestValidator()
    {
        RuleFor(c => c.UserId).NotEmpty().MaximumLength(64);
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.IssuedFor).MaximumLength(200)
            .When(c => !string.IsNullOrWhiteSpace(c.IssuedFor));
        RuleFor(c => c.StorageKey).MaximumLength(500)
            .When(c => !string.IsNullOrWhiteSpace(c.StorageKey));
    }
}
