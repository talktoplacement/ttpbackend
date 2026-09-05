using CareerPlatform.Api.Features.Learning.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Learning.Validation;

public sealed class UpsertProgressRequestValidator : AbstractValidator<UpsertProgressRequest>
{
    private static readonly string[] AllowedStatuses = { "not-started", "in-progress", "completed" };

    public UpsertProgressRequestValidator()
    {
        RuleFor(c => c.PercentComplete).InclusiveBetween(0, 100);
        RuleFor(c => c.Status).Must(s =>
                string.IsNullOrWhiteSpace(s) ||
                AllowedStatuses.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");
        RuleFor(c => c.Notes).MaximumLength(2000)
            .When(c => !string.IsNullOrWhiteSpace(c.Notes));
    }
}
