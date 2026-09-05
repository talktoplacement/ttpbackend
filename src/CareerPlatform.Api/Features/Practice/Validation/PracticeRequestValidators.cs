using CareerPlatform.Api.Features.Practice.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Practice.Validation;

public sealed class CreatePracticeQuestionRequestValidator : AbstractValidator<CreatePracticeQuestionRequest>
{
    private static readonly string[] AllowedDifficulties = { "Easy", "Medium", "Hard" };
    public CreatePracticeQuestionRequestValidator()
    {
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$");
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(8000);
        RuleFor(c => c.Difficulty).NotEmpty()
            .Must(d => AllowedDifficulties.Contains(d))
            .WithMessage($"Difficulty must be one of: {string.Join(", ", AllowedDifficulties)}.");
        RuleFor(c => c.Category).NotEmpty().MaximumLength(64);
        RuleFor(c => c.AcceptanceRate).InclusiveBetween(0, 100);
    }
}

public sealed class UpdatePracticeQuestionRequestValidator : AbstractValidator<UpdatePracticeQuestionRequest>
{
    private static readonly string[] AllowedDifficulties = { "Easy", "Medium", "Hard" };
    public UpdatePracticeQuestionRequestValidator()
    {
        RuleFor(c => c.Slug).MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(c => !string.IsNullOrWhiteSpace(c.Slug));
        RuleFor(c => c.Title).MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(8000);
        RuleFor(c => c.Difficulty).Must(d => d is null || AllowedDifficulties.Contains(d))
            .WithMessage($"Difficulty must be one of: {string.Join(", ", AllowedDifficulties)}.");
        RuleFor(c => c.Category).MaximumLength(64);
        RuleFor(c => c.AcceptanceRate).InclusiveBetween(0, 100).When(c => c.AcceptanceRate.HasValue);
    }
}

public sealed class ToggleBookmarkRequestValidator : AbstractValidator<ToggleBookmarkRequest>
{
    public ToggleBookmarkRequestValidator() =>
        RuleFor(c => c.Notes).MaximumLength(1000).When(c => !string.IsNullOrWhiteSpace(c.Notes));
}
