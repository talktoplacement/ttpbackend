using CareerPlatform.Api.Features.MentorPortal.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.MentorPortal.Validation;

public sealed class UpdateMentorProfileRequestValidator : AbstractValidator<UpdateMentorProfileRequest>
{
    public UpdateMentorProfileRequestValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Company).MaximumLength(100);
        RuleFor(c => c.Role).MaximumLength(100);
        RuleFor(c => c.YearsOfExperience).MaximumLength(50);
        RuleFor(c => c.Bio).MaximumLength(4000);
        RuleFor(c => c.AvatarUrl).MaximumLength(500);
        RuleFor(c => c.PricePerSession).GreaterThanOrEqualTo(0);
        RuleForEach(c => c.Expertise).MaximumLength(60);
    }
}

public sealed class CreateMentorSlotRequestValidator : AbstractValidator<CreateMentorSlotRequest>
{
    public CreateMentorSlotRequestValidator()
    {
        RuleFor(c => c.EndTimeUtc).GreaterThan(c => c.StartTimeUtc)
            .WithMessage("The slot end time must be after its start time.");
        RuleFor(c => c.MeetingLink).MaximumLength(500);
    }
}
