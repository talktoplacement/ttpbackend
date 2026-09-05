using CareerPlatform.Api.Features.Skills.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Skills.Validation;

public sealed class ReplaceSkillsRequestValidator : AbstractValidator<ReplaceSkillsRequest>
{
    private static readonly string[] AllowedLevels =
        { "Beginner", "Intermediate", "Advanced", "Expert" };

    public ReplaceSkillsRequestValidator()
    {
        RuleFor(r => r.Skills).NotNull();
        RuleForEach(r => r.Skills).ChildRules(item =>
        {
            item.RuleFor(s => s.SkillName).NotEmpty().MaximumLength(100);
            item.RuleFor(s => s.Category).NotEmpty().MaximumLength(64);
            item.RuleFor(s => s.ProficiencyLevel).NotEmpty()
                .Must(p => AllowedLevels.Contains(p, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"ProficiencyLevel must be one of: {string.Join(", ", AllowedLevels)}.");
            item.RuleFor(s => s.DisplayOrder).GreaterThanOrEqualTo(0);
        });
        RuleFor(r => r.Skills.Count).LessThanOrEqualTo(200)
            .WithMessage("A profile may declare at most 200 skills.");
    }
}
