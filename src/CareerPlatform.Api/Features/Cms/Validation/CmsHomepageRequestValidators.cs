using CareerPlatform.Api.Features.Cms.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Cms.Validation;

public sealed class UpdateCmsHomepageRequestValidator : AbstractValidator<UpdateCmsHomepageRequest>
{
    public UpdateCmsHomepageRequestValidator()
    {
        RuleFor(c => c.HeroTitle).NotEmpty().MaximumLength(200);
        RuleFor(c => c.HeroSubtitle).MaximumLength(500);
        RuleFor(c => c.PrimaryCtaLabel).MaximumLength(64);
        RuleFor(c => c.PrimaryCtaHref).MaximumLength(500);
        RuleFor(c => c.SecondaryCtaLabel).MaximumLength(64);
        RuleFor(c => c.SecondaryCtaHref).MaximumLength(500);
    }
}
