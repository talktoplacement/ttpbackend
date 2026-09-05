using CareerPlatform.Api.Features.Interviews.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Interviews.Validation;

public sealed class CreateInterviewQuestionRequestValidator : AbstractValidator<CreateInterviewQuestionRequest>
{
    public CreateInterviewQuestionRequestValidator()
    {
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").WithMessage("Slug must be kebab-case.");
        RuleFor(c => c.Prompt).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.ExpectedAnswer).MaximumLength(8000);
        RuleFor(c => c.Topic).NotEmpty().MaximumLength(120);
        RuleFor(c => c.Difficulty).NotEmpty().MaximumLength(32);
    }
}

public sealed class UpdateInterviewQuestionRequestValidator : AbstractValidator<UpdateInterviewQuestionRequest>
{
    public UpdateInterviewQuestionRequestValidator()
    {
        RuleFor(c => c.Slug).MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(c => !string.IsNullOrWhiteSpace(c.Slug))
            .WithMessage("Slug must be kebab-case.");
        RuleFor(c => c.Prompt).MaximumLength(4000);
        RuleFor(c => c.ExpectedAnswer).MaximumLength(8000);
        RuleFor(c => c.Topic).MaximumLength(120);
        RuleFor(c => c.Difficulty).MaximumLength(32);
    }
}

public sealed class CreateInterviewSessionRequestValidator : AbstractValidator<CreateInterviewSessionRequest>
{
    public CreateInterviewSessionRequestValidator()
    {
        RuleFor(c => c.Type).NotEmpty().MaximumLength(64);
        RuleFor(c => c.Topic).NotEmpty().MaximumLength(120);
        RuleFor(c => c.DurationMinutes).InclusiveBetween(5, 240);
    }
}

public sealed class UpdateInterviewSessionRequestValidator : AbstractValidator<UpdateInterviewSessionRequest>
{
    public UpdateInterviewSessionRequestValidator()
    {
        RuleFor(c => c.Status).MaximumLength(32);
        RuleFor(c => c.Score).InclusiveBetween(0, 100).When(c => c.Score.HasValue);
        RuleFor(c => c.RubricReportJson).MaximumLength(20_000)
            .When(c => !string.IsNullOrWhiteSpace(c.RubricReportJson));
    }
}
