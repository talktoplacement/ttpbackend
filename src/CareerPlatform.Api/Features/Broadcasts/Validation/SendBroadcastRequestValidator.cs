using CareerPlatform.Api.Features.Broadcasts.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Broadcasts.Validation;

public sealed class SendBroadcastRequestValidator : AbstractValidator<SendBroadcastRequest>
{
    private static readonly string[] AllowedTypes = { "Notification", "Promotion" };

    public SendBroadcastRequestValidator()
    {
        RuleFor(c => c.BroadcastType)
            .NotEmpty()
            .Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("BroadcastType must be 'Notification' or 'Promotion'.");
        RuleFor(c => c.Heading).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Message).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.TargetPlan).MaximumLength(64);
        RuleFor(c => c.QuestionText).MaximumLength(2000);
        RuleFor(c => c.QuestionLink).MaximumLength(500);
    }
}
