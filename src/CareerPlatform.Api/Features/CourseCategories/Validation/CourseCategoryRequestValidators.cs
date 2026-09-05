using CareerPlatform.Api.Features.CourseCategories.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.CourseCategories.Validation;

public sealed class CreateCourseCategoryRequestValidator : AbstractValidator<CreateCourseCategoryRequest>
{
    public CreateCourseCategoryRequestValidator()
    {
        RuleFor(r => r.Slug).NotEmpty().MaximumLength(64)
            .Matches(@"^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens.");
        RuleFor(r => r.Name).NotEmpty().MaximumLength(128);
        RuleFor(r => r.Description).MaximumLength(500);
        RuleFor(r => r.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}

public sealed class UpdateCourseCategoryRequestValidator : AbstractValidator<UpdateCourseCategoryRequest>
{
    public UpdateCourseCategoryRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(128);
        RuleFor(r => r.Description).MaximumLength(500);
        RuleFor(r => r.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
