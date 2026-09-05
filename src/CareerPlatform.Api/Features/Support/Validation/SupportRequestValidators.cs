using CareerPlatform.Api.Features.Support.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Support.Validation;

public sealed class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    private static readonly string[] AllowedCategories =
        { "Billing", "Technical", "Mentorship", "Curriculum", "Other" };
    private static readonly string[] AllowedPriorities = { "low", "normal", "high", "urgent" };

    public CreateTicketRequestValidator()
    {
        RuleFor(c => c.Subject).NotEmpty().MaximumLength(200);
        RuleFor(c => c.Category).NotEmpty()
            .Must(cat => AllowedCategories.Contains(cat, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Category must be one of: {string.Join(", ", AllowedCategories)}.");
        RuleFor(c => c.Priority).Must(p =>
                string.IsNullOrWhiteSpace(p) ||
                AllowedPriorities.Contains(p.Trim().ToLowerInvariant()))
            .WithMessage($"Priority must be one of: {string.Join(", ", AllowedPriorities)}.");
        RuleFor(c => c.Body).NotEmpty().MaximumLength(10_000);
    }
}

public sealed class PostTicketMessageRequestValidator : AbstractValidator<PostTicketMessageRequest>
{
    public PostTicketMessageRequestValidator() =>
        RuleFor(c => c.Body).NotEmpty().MaximumLength(10_000);
}

public sealed class UpdateTicketStatusRequestValidator : AbstractValidator<UpdateTicketStatusRequest>
{
    private static readonly string[] AllowedStatuses = { "open", "pending", "resolved", "closed" };
    public UpdateTicketStatusRequestValidator()
    {
        RuleFor(c => c.Status).NotEmpty()
            .Must(s => AllowedStatuses.Contains(s.Trim().ToLowerInvariant()))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");
        RuleFor(c => c.AssignedToUserId).MaximumLength(64)
            .When(c => !string.IsNullOrWhiteSpace(c.AssignedToUserId));
    }
}
