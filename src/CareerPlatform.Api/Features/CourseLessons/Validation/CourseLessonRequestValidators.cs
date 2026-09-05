using CareerPlatform.Api.Features.CourseLessons.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.CourseLessons.Validation;

internal static class CourseLessonRules
{
    public static readonly string[] AllowedTypes = { "video", "article", "quiz" };

    public static IRuleBuilderOptions<T, string> LessonType<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"LessonType must be one of: {string.Join(", ", AllowedTypes)}.");
}

public sealed class CreateCourseLessonRequestValidator : AbstractValidator<CreateCourseLessonRequest>
{
    public CreateCourseLessonRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.LessonType).LessonType();
        RuleFor(r => r.DurationSeconds).GreaterThan(0).When(r => r.DurationSeconds.HasValue);
        RuleFor(r => r.ContentUrl).MaximumLength(1000);
        RuleFor(r => r.ContentMarkdown).MaximumLength(8000);
        RuleFor(r => r.OrderIndex).GreaterThanOrEqualTo(0);
        // A video lesson needs a URL; an article needs markdown. Enforced here so the client
        // can't create an unrenderable lesson.
        RuleFor(r => r).Must(r =>
                !string.Equals(r.LessonType, "video", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(r.ContentUrl))
            .WithMessage("A video lesson requires ContentUrl.");
        RuleFor(r => r).Must(r =>
                !string.Equals(r.LessonType, "article", StringComparison.OrdinalIgnoreCase)
                || !string.IsNullOrWhiteSpace(r.ContentMarkdown))
            .WithMessage("An article lesson requires ContentMarkdown.");
    }
}

public sealed class UpdateCourseLessonRequestValidator : AbstractValidator<UpdateCourseLessonRequest>
{
    public UpdateCourseLessonRequestValidator()
    {
        RuleFor(r => r.Title).NotEmpty().MaximumLength(200);
        RuleFor(r => r.LessonType).LessonType();
        RuleFor(r => r.DurationSeconds).GreaterThan(0).When(r => r.DurationSeconds.HasValue);
        RuleFor(r => r.ContentUrl).MaximumLength(1000);
        RuleFor(r => r.ContentMarkdown).MaximumLength(8000);
        RuleFor(r => r.OrderIndex).GreaterThanOrEqualTo(0);
    }
}

public sealed class ReorderCourseLessonsRequestValidator
    : AbstractValidator<ReorderCourseLessonsRequest>
{
    public ReorderCourseLessonsRequestValidator()
    {
        RuleFor(r => r.OrderedIds).NotNull().NotEmpty()
            .WithMessage("OrderedIds must contain at least one lesson id.");
        RuleFor(r => r.OrderedIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("OrderedIds must not contain duplicates.");
    }
}
