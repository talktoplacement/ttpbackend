using CareerPlatform.Api.Features.Meetings.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Meetings.Validation;

public sealed class UpdateMeetingRequestValidator : AbstractValidator<UpdateMeetingRequest>
{
    private static readonly string[] AllowedStatuses = { "Scheduled", "In Progress", "Completed", "Cancelled" };

    public UpdateMeetingRequestValidator()
    {
        RuleFor(c => c.Id).GreaterThan(0);
        RuleFor(c => c.Status).Must(s => s is null || AllowedStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");
        RuleFor(c => c.MeetUrl).MaximumLength(500);
        RuleFor(c => c.ScheduledAt)
            .Must(v => string.IsNullOrEmpty(v) || DateTime.TryParse(v, out _))
            .WithMessage("ScheduledAt must be a valid ISO-8601 timestamp.");
        RuleFor(c => c)
            .Must(c => c.Status is not null || c.ScheduledAt is not null || c.MeetUrl is not null)
            .WithMessage("At least one field must be provided.");
    }
}
