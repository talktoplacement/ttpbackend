using CareerPlatform.Api.Features.PracticeBanks.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.PracticeBanks.Validation;

public sealed class CreatePracticeBankRequestValidator : AbstractValidator<CreatePracticeBankRequest>
{
    public CreatePracticeBankRequestValidator()
    {
        RuleFor(r => r.Slug).NotEmpty().MaximumLength(64)
            .Matches(@"^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens.");
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Description).MaximumLength(1000);
    }
}

public sealed class UpdatePracticeBankRequestValidator : AbstractValidator<UpdatePracticeBankRequest>
{
    public UpdatePracticeBankRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Description).MaximumLength(1000);
    }
}

public sealed class SetBankQuestionsRequestValidator : AbstractValidator<SetBankQuestionsRequest>
{
    public SetBankQuestionsRequestValidator()
    {
        RuleFor(r => r.QuestionIds).NotNull();
        RuleFor(r => r.QuestionIds)
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("QuestionIds must not contain duplicates.");
        RuleFor(r => r.QuestionIds.Count).LessThanOrEqualTo(500)
            .WithMessage("A bank may hold at most 500 questions.");
        RuleForEach(r => r.QuestionIds).GreaterThan(0);
    }
}
