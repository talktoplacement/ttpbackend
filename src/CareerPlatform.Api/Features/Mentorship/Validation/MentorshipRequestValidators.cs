using CareerPlatform.Api.Features.Mentorship.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Mentorship.Validation;

public sealed class BookMentorSlotRequestValidator : AbstractValidator<BookMentorSlotRequest>
{
    public BookMentorSlotRequestValidator()
    {
        RuleFor(c => c.MentorId).GreaterThan(0);
        RuleFor(c => c)
            .Must(c => c.SlotId.HasValue || !string.IsNullOrWhiteSpace(c.SlotTime))
            .WithMessage("Provide either SlotId or SlotTime.");
        RuleFor(c => c.SlotTime)
            .Must(v => string.IsNullOrEmpty(v) || DateTime.TryParse(v, out _))
            .WithMessage("SlotTime must be a valid ISO-8601 timestamp.");
        RuleFor(c => c.Notes).MaximumLength(1000);
    }
}

public sealed class CreateMentorSlotsRequestValidator : AbstractValidator<CreateMentorSlotsRequest>
{
    public CreateMentorSlotsRequestValidator()
    {
        RuleFor(c => c.MentorId).GreaterThan(0);
        RuleFor(c => c.StartTimes).NotEmpty()
            .WithMessage("Provide at least one slot start time.");
        RuleForEach(c => c.StartTimes!)
            .NotEmpty()
            .Must(v => DateTime.TryParse(v, out _))
            .WithMessage("Each start time must be a valid ISO-8601 timestamp.");
    }
}

public sealed class OnboardMentorRequestValidator : AbstractValidator<OnboardMentorRequest>
{
    public OnboardMentorRequestValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(150);
        RuleFor(c => c.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(c => c.Role).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Company).NotEmpty().MaximumLength(100);
        RuleFor(c => c.YearsOfExperience).InclusiveBetween(0, 60);
        RuleFor(c => c.HourlyRateInr).GreaterThanOrEqualTo(0)
            .When(c => c.HourlyRateInr.HasValue);
        RuleFor(c => c.Bio).MaximumLength(2000);
        RuleFor(c => c.AvatarUrl).MaximumLength(500);
    }
}

public sealed class UpdateMentorRequestValidator : AbstractValidator<UpdateMentorRequest>
{
    private static readonly string[] AllowedStatuses = { "Verified", "Pending", "Suspended" };

    public UpdateMentorRequestValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0);
        RuleFor(c => c.Status).Must(s => s is null || AllowedStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");
        RuleFor(c => c.Name).MaximumLength(150);
        RuleFor(c => c.Role).MaximumLength(100);
        RuleFor(c => c.Company).MaximumLength(100);
        RuleFor(c => c.YearsOfExperience).InclusiveBetween(0, 60)
            .When(c => c.YearsOfExperience.HasValue);
        RuleFor(c => c.HourlyRateInr).GreaterThanOrEqualTo(0)
            .When(c => c.HourlyRateInr.HasValue);
        RuleFor(c => c.Bio).MaximumLength(2000);
        RuleFor(c => c.AvatarUrl).MaximumLength(500);
    }
}

/// <summary>
/// Guards a session rating. The 1–5 bound matches the <c>MentorReview.Rating</c> contract and the
/// star UI; a 0 or a 6 would silently skew the mentor's average, which is why it is rejected here
/// rather than clamped.
/// </summary>
public sealed class SubmitMentorReviewRequestValidator : AbstractValidator<SubmitMentorReviewRequest>
{
    public SubmitMentorReviewRequestValidator()
    {
        RuleFor(c => c.Rating).InclusiveBetween(1, 5);
        RuleFor(c => c.Comment).MaximumLength(2000);
    }
}
