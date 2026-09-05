using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.Assessments.Domain;
using CareerPlatform.Api.Features.Assessments.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Assessments.Validation;

/// <summary>
/// Validates one authored test case. Blank expected output is allowed (a program may legitimately
/// print nothing) but a case with neither input nor expected output is authoring noise.
/// </summary>
public sealed class AuthoredTestCaseValidator : AbstractValidator<AuthoredTestCase>
{
    /// <summary>Bounds a single case so one pathological paste cannot bloat every grading run.</summary>
    private const int MaxFieldLength = 20_000;

    public AuthoredTestCaseValidator()
    {
        RuleFor(c => c.Input).MaximumLength(MaxFieldLength);
        RuleFor(c => c.ExpectedOutput).MaximumLength(MaxFieldLength);
        RuleFor(c => c.Weight).InclusiveBetween(1, 1000);
        RuleFor(c => c)
            .Must(c => !string.IsNullOrWhiteSpace(c.Input) || !string.IsNullOrWhiteSpace(c.ExpectedOutput))
            .WithMessage("A test case must specify an input or an expected output.");
    }
}

/// <summary>
/// Validates one authored question.
///
/// The type-conditional rules are the important part: an MCQ without a correct option, or a coding
/// question without a single hidden case, would be ungradable and would silently award every student
/// zero. Rejecting it at authoring time is the only place that failure is cheap.
/// </summary>
public sealed class AuthoredQuestionValidator : AbstractValidator<AuthoredQuestion>
{
    public AuthoredQuestionValidator(IOptions<CodeExecutionOptions> codeExecution)
    {
        ArgumentNullException.ThrowIfNull(codeExecution);
        var maxTimeLimit = codeExecution.Value.MaxTimeLimitMs;

        RuleFor(q => q.QuestionType)
            .NotEmpty()
            .Must(AssessmentQuestionType.IsSupported)
            .WithMessage(
                $"'Question Type' must be '{AssessmentQuestionType.MultipleChoice}' or " +
                $"'{AssessmentQuestionType.Coding}'.");
        RuleFor(q => q.Title).NotEmpty().MaximumLength(300);
        RuleFor(q => q.PromptMarkdown).MaximumLength(20_000);
        RuleFor(q => q.Marks).InclusiveBetween(1, 1000);
        RuleFor(q => q.TimeLimitMs).InclusiveBetween(100, maxTimeLimit)
            .When(q => q.TimeLimitMs.HasValue);
        RuleForEach(q => q.TestCases).SetValidator(new AuthoredTestCaseValidator());

        When(IsMultipleChoice, () =>
        {
            RuleFor(q => q.Options).NotNull()
                .Must(o => o is { Count: >= 2 } and { Count: <= 26 })
                .WithMessage("A multiple-choice question needs between 2 and 26 options.");
            RuleForEach(q => q.Options).NotEmpty().MaximumLength(1000);
            RuleFor(q => q.CorrectOptionIndex).NotNull()
                .WithMessage("A multiple-choice question must declare its correct option.");
            RuleFor(q => q)
                .Must(q => q.CorrectOptionIndex is null || q.Options is null ||
                           (q.CorrectOptionIndex >= 0 && q.CorrectOptionIndex < q.Options.Count))
                .WithMessage("'Correct Option Index' must point at one of the supplied options.");
        });

        When(q => !IsMultipleChoice(q), () =>
        {
            RuleFor(q => q.FunctionName).MaximumLength(128);
            RuleFor(q => q.TestCases).NotNull()
                .Must(t => t is { Count: > 0 })
                .WithMessage("A coding question needs at least one test case.");
            RuleFor(q => q.TestCases)
                .Must(t => t is null || t.Any(c => !c.IsSample))
                .WithMessage(
                    "A coding question needs at least one non-sample (hidden) test case, otherwise " +
                    "students can iterate against the full grading set.");
            RuleForEach(q => q.StarterCode!.Values).MaximumLength(20_000)
                .When(q => q.StarterCode is not null);
        });
    }

    private static bool IsMultipleChoice(AuthoredQuestion q) =>
        !string.Equals(q.QuestionType, AssessmentQuestionType.Coding, StringComparison.OrdinalIgnoreCase);
}

/// <summary>Validates the whole-bank replace payload.</summary>
public sealed class ReplaceQuestionBankRequestValidator : AbstractValidator<ReplaceQuestionBankRequest>
{
    public ReplaceQuestionBankRequestValidator(IOptions<CodeExecutionOptions> codeExecution)
    {
        RuleFor(r => r.Questions).NotNull()
            .Must(q => q is { Count: > 0 })
            .WithMessage("A question bank must contain at least one question.")
            .Must(q => q is null || q.Count <= 200)
            .WithMessage("A question bank is limited to 200 questions.");
        RuleForEach(r => r.Questions).SetValidator(new AuthoredQuestionValidator(codeExecution));
    }
}
