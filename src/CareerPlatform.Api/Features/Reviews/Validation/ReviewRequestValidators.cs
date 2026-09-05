using CareerPlatform.Api.Features.Reviews.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Reviews.Validation;

public sealed class CreateReviewRequestValidator : AbstractValidator<CreateReviewRequest>
{
    public CreateReviewRequestValidator()
    {
        RuleFor(r => r.CourseId).GreaterThan(0);
        RuleFor(r => r.Rating).InclusiveBetween(1, 5);
        RuleFor(r => r.Comment).NotEmpty().MinimumLength(10).MaximumLength(2000);
    }
}

public sealed class ModerateReviewRequestValidator : AbstractValidator<ModerateReviewRequest>
{
    private static readonly string[] Allowed = { "approve", "reject" };
    public ModerateReviewRequestValidator() =>
        RuleFor(r => r.Action).NotEmpty()
            .Must(a => Allowed.Contains(a, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Action must be one of: {string.Join(", ", Allowed)}.");
}
