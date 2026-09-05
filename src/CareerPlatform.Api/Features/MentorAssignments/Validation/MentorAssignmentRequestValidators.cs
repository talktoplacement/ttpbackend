using CareerPlatform.Api.Features.MentorAssignments.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.MentorAssignments.Validation;

public sealed class CreateMentorAssignmentRequestValidator
    : AbstractValidator<CreateMentorAssignmentRequest>
{
    public CreateMentorAssignmentRequestValidator()
    {
        RuleFor(r => r.StudentUserId).NotEmpty().MaximumLength(64);
        RuleFor(r => r.MentorId).GreaterThan(0);
        RuleFor(r => r.CohortName).MaximumLength(128);
        RuleFor(r => r.Notes).MaximumLength(1000);
    }
}

public sealed class UpdateMentorAssignmentRequestValidator
    : AbstractValidator<UpdateMentorAssignmentRequest>
{
    public UpdateMentorAssignmentRequestValidator()
    {
        RuleFor(r => r.CohortName).MaximumLength(128);
        RuleFor(r => r.Notes).MaximumLength(1000);
    }
}
