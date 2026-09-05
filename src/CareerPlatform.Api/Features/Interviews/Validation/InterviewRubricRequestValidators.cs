using CareerPlatform.Api.Features.Interviews.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Interviews.Validation;

public sealed class UpsertInterviewRubricRequestValidator : AbstractValidator<UpsertInterviewRubricRequest>
{
    public UpsertInterviewRubricRequestValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Description).MaximumLength(2000);
        RuleFor(c => c.Weight).InclusiveBetween(0, 100);
        RuleFor(c => c.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
