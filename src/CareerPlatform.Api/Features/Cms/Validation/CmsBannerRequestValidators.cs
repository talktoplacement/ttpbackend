using CareerPlatform.Api.Features.Cms.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Cms.Validation;

public sealed class UpsertCmsBannerRequestValidator : AbstractValidator<UpsertCmsBannerRequest>
{
    private static readonly string[] AllowedTones = { "info", "success", "warning", "danger" };

    public UpsertCmsBannerRequestValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Message).NotEmpty().MaximumLength(500);
        RuleFor(c => c.LinkUrl).MaximumLength(500);
        RuleFor(c => c.Tone).NotEmpty().Must(t => AllowedTones.Contains(t))
            .WithMessage($"Tone must be one of: {string.Join(", ", AllowedTones)}.");
        RuleFor(c => c.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
