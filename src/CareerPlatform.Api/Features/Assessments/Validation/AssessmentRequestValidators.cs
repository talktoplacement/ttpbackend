using System.Globalization;
using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.Assessments.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Assessments.Validation;

/// <summary>
/// Shared rules for the ISO-8601 window fields. Centralised because the create and update contracts
/// must agree: the service parses these with <c>TryParse</c> and silently drops anything unparseable,
/// so the rejection has to happen here or a typo would quietly clear a scheduled window.
/// </summary>
internal static class AssessmentWindowRules
{
    internal static bool IsParseableDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ||
        DateTime.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out _);

    internal const string DateMessage = "'{PropertyName}' must be a valid ISO-8601 date/time.";
}

public sealed class CreateAssessmentRequestValidator : AbstractValidator<CreateAssessmentRequest>
{
    public CreateAssessmentRequestValidator()
    {
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$");
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(4000);
        RuleFor(c => c.DurationMinutes).InclusiveBetween(1, 600);
        RuleFor(c => c.TotalMarks).GreaterThan(0);
        RuleFor(c => c.PassingMarks).GreaterThanOrEqualTo(0);
        RuleFor(c => c.QuestionsCount).GreaterThan(0);
        RuleFor(c => c.Category).NotEmpty().MaximumLength(64);
        RuleFor(c => c.PassingMarks).LessThanOrEqualTo(c => c.TotalMarks)
            .WithMessage("'Passing Marks' cannot exceed 'Total Marks'.");
        RuleFor(c => c.StartsAtUtc).Must(AssessmentWindowRules.IsParseableDate)
            .WithMessage(AssessmentWindowRules.DateMessage);
        RuleFor(c => c.EndsAtUtc).Must(AssessmentWindowRules.IsParseableDate)
            .WithMessage(AssessmentWindowRules.DateMessage);
    }
}

public sealed class UpdateAssessmentRequestValidator : AbstractValidator<UpdateAssessmentRequest>
{
    public UpdateAssessmentRequestValidator()
    {
        RuleFor(c => c.Slug).MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(c => !string.IsNullOrWhiteSpace(c.Slug));
        RuleFor(c => c.Title).MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(4000);
        RuleFor(c => c.DurationMinutes).InclusiveBetween(1, 600).When(c => c.DurationMinutes.HasValue);
        RuleFor(c => c.TotalMarks).GreaterThan(0).When(c => c.TotalMarks.HasValue);
        RuleFor(c => c.PassingMarks).GreaterThanOrEqualTo(0).When(c => c.PassingMarks.HasValue);
        RuleFor(c => c.QuestionsCount).GreaterThan(0).When(c => c.QuestionsCount.HasValue);
        RuleFor(c => c.Category).MaximumLength(64);
        RuleFor(c => c.StartsAtUtc).Must(AssessmentWindowRules.IsParseableDate)
            .WithMessage(AssessmentWindowRules.DateMessage);
        RuleFor(c => c.EndsAtUtc).Must(AssessmentWindowRules.IsParseableDate)
            .WithMessage(AssessmentWindowRules.DateMessage);
    }
}

/// <summary>
/// Guards the autosave payload. An answer carries either a chosen option (MCQ) or source code
/// (coding); which one is meaningful is decided by the question's type server-side, so this validator
/// only bounds the shapes rather than trying to infer intent.
/// </summary>
public sealed class SaveAnswerRequestValidator : AbstractValidator<SaveAnswerRequest>
{
    public SaveAnswerRequestValidator(IOptions<CodeExecutionOptions> codeExecution)
    {
        ArgumentNullException.ThrowIfNull(codeExecution);
        var maxSource = codeExecution.Value.MaxSourceCodeLength;

        RuleFor(c => c.QuestionId).GreaterThan(0);
        RuleFor(c => c.SelectedOptionIndex).InclusiveBetween(0, 63)
            .When(c => c.SelectedOptionIndex.HasValue);
        RuleFor(c => c.Language).MaximumLength(32);
        RuleFor(c => c.SourceCode).MaximumLength(maxSource);
    }
}

/// <summary>Guards the interactive "Run" payload; the language must actually be offered.</summary>
public sealed class RunCodeRequestValidator : AbstractValidator<RunCodeRequest>
{
    public RunCodeRequestValidator(IOptions<CodeExecutionOptions> codeExecution)
    {
        ArgumentNullException.ThrowIfNull(codeExecution);
        var maxSource = codeExecution.Value.MaxSourceCodeLength;

        RuleFor(c => c.QuestionId).GreaterThan(0);
        RuleFor(c => c.Language).NotEmpty().MaximumLength(32);
        RuleFor(c => c.SourceCode).NotEmpty().MaximumLength(maxSource);
    }
}
