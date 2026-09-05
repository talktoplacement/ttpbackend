using CareerPlatform.Api.Features.Resumes.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Resumes.Validation;

public sealed class CreateResumeDraftRequestValidator : AbstractValidator<CreateResumeDraftRequest>
{
    public CreateResumeDraftRequestValidator()
    {
        RuleFor(d => d.Title).NotEmpty().MaximumLength(200);
        RuleFor(d => d.TemplateCode).NotEmpty().MaximumLength(64);
    }
}

public sealed class UpdateResumeDraftRequestValidator : AbstractValidator<UpdateResumeDraftRequest>
{
    public UpdateResumeDraftRequestValidator()
    {
        RuleFor(d => d.Title).NotEmpty().MaximumLength(200).When(d => d.Title is not null);
        RuleFor(d => d.TemplateCode).NotEmpty().MaximumLength(64).When(d => d.TemplateCode is not null);

        // An empty PUT would bump LastEditedAt without changing anything, making the "last saved"
        // timestamp a lie. Require at least one field.
        RuleFor(d => d)
            .Must(d => d.Title is not null || d.TemplateCode is not null || d.Content is not null)
            .WithMessage("Provide at least one of title, templateCode or content.");
    }
}
