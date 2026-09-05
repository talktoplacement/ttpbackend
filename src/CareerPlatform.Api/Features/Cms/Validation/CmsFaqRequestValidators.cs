using CareerPlatform.Api.Features.Cms.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Cms.Validation;

public sealed class UpsertCmsFaqRequestValidator : AbstractValidator<UpsertCmsFaqRequest>
{
    public UpsertCmsFaqRequestValidator()
    {
        RuleFor(r => r.Question).NotEmpty().MinimumLength(5).MaximumLength(500);
        RuleFor(r => r.Answer).NotEmpty().MinimumLength(5).MaximumLength(4000);
        RuleFor(r => r.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpsertCmsTestimonialRequestValidator
    : AbstractValidator<UpsertCmsTestimonialRequest>
{
    public UpsertCmsTestimonialRequestValidator()
    {
        RuleFor(r => r.AuthorName).NotEmpty().MaximumLength(128);
        RuleFor(r => r.AuthorRole).MaximumLength(200);
        RuleFor(r => r.Quote).NotEmpty().MinimumLength(10).MaximumLength(2000);
        RuleFor(r => r.AvatarUrl).MaximumLength(500);
        RuleFor(r => r.Rating).InclusiveBetween(1, 5).When(r => r.Rating.HasValue);
        RuleFor(r => r.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpsertCmsNavigationLinkRequestValidator
    : AbstractValidator<UpsertCmsNavigationLinkRequest>
{
    private static readonly string[] AllowedGroups = { "header", "footer", "mobile" };

    public UpsertCmsNavigationLinkRequestValidator()
    {
        RuleFor(r => r.Label).NotEmpty().MaximumLength(64);
        RuleFor(r => r.Href).NotEmpty().MaximumLength(500);
        RuleFor(r => r.GroupName).NotEmpty()
            .Must(g => AllowedGroups.Contains(g, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"GroupName must be one of: {string.Join(", ", AllowedGroups)}.");
        RuleFor(r => r.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
