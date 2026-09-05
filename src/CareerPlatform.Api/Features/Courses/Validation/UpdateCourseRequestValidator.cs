using CareerPlatform.Api.Features.Courses.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Courses.Validation;

/// <summary>
/// Validates <see cref="UpdateCourseRequest"/>. Slug uniqueness ("slug owned by a different
/// course") is DB-checked in <see cref="Service.CourseService"/>.
/// </summary>
public sealed class UpdateCourseRequestValidator : AbstractValidator<UpdateCourseRequest>
{
    public UpdateCourseRequestValidator()
    {
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase kebab-case (e.g. 'full-stack-bootcamp').");
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(4000);
        RuleFor(c => c.MediaUrl).MaximumLength(2000)
            .When(c => !string.IsNullOrWhiteSpace(c.MediaUrl));
        RuleFor(c => c.Price).GreaterThanOrEqualTo(0m);
        RuleFor(c => c.Currency).MaximumLength(3)
            .When(c => !string.IsNullOrWhiteSpace(c.Currency));
    }
}
