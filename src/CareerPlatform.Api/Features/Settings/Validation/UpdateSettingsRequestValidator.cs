using CareerPlatform.Api.Features.Settings.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Settings.Validation;

/// <summary>
/// Validates the batch update body: at least one entry, no null/empty keys, and no key repeated.
/// Value semantics (typed parsing per key) are still enforced inside the service so an "unknown key"
/// or "value out of range" surfaces with the exact key name.
/// </summary>
public sealed class UpdateSettingsRequestValidator : AbstractValidator<UpdateSettingsRequest>
{
    public UpdateSettingsRequestValidator()
    {
        RuleFor(r => r.Updates)
            .NotNull()
            .Must(u => u.Count >= 1)
            .WithMessage("Provide at least one setting to update.")
            .Must(u => u.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count() == u.Count)
            .WithMessage("Each setting key must appear at most once in a batch.");

        RuleForEach(r => r.Updates).ChildRules(entry =>
        {
            entry.RuleFor(e => e.Key).NotEmpty().MaximumLength(120);
            entry.RuleFor(e => e.Value).NotNull().MaximumLength(4000);
        });
    }
}
