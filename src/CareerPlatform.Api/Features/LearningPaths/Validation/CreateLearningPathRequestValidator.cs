using CareerPlatform.Api.Features.LearningPaths.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.LearningPaths.Validation;

public sealed class CreateLearningPathRequestValidator : AbstractValidator<CreateLearningPathRequest>
{
    public CreateLearningPathRequestValidator()
    {
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be kebab-case.");
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(4000);
        RuleFor(c => c.TargetRole).NotEmpty().MaximumLength(120);
        RuleFor(c => c.EstimatedMonths).InclusiveBetween(1, 120);
    }
}
