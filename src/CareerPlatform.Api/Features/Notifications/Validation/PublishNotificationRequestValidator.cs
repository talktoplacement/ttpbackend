using CareerPlatform.Api.Features.Notifications.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Notifications.Validation;

public sealed class PublishNotificationRequestValidator : AbstractValidator<PublishNotificationRequest>
{
    public PublishNotificationRequestValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Body).NotEmpty().MaximumLength(4000);
        RuleFor(c => c.Type).MaximumLength(32).When(c => !string.IsNullOrWhiteSpace(c.Type));
        RuleFor(c => c.TargetRole).MaximumLength(32).When(c => !string.IsNullOrWhiteSpace(c.TargetRole));
        RuleFor(c => c.ActionUrl).MaximumLength(500).When(c => !string.IsNullOrWhiteSpace(c.ActionUrl));
    }
}
