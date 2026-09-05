using CareerPlatform.Api.Features.Content.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Content.Validation;

public sealed class CreateLanguageRequestValidator : AbstractValidator<CreateLanguageRequest>
{
    public CreateLanguageRequestValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(100)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase kebab-case (letters, digits, single dashes).");
        RuleFor(c => c.Description).MaximumLength(1000);
        RuleFor(c => c.Price).InclusiveBetween(0m, 1_000_000m);
    }
}

public sealed class UpdateLanguageRequestValidator : AbstractValidator<UpdateLanguageRequest>
{
    public UpdateLanguageRequestValidator()
    {
        RuleFor(c => c.Title).MaximumLength(100).When(c => c.Title is not null);
        RuleFor(c => c.Slug).MaximumLength(100)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$").When(c => !string.IsNullOrWhiteSpace(c.Slug))
            .WithMessage("Slug must be lowercase kebab-case (letters, digits, single dashes).");
        RuleFor(c => c.Description).MaximumLength(1000).When(c => c.Description is not null);
        RuleFor(c => c.Price).InclusiveBetween(0m, 1_000_000m).When(c => c.Price.HasValue);
    }
}

public sealed class CreateSectionRequestValidator : AbstractValidator<CreateSectionRequest>
{
    public CreateSectionRequestValidator()
    {
        RuleFor(c => c.LanguageId).GreaterThan(0);
        RuleFor(c => c.Title).NotEmpty().MaximumLength(100);
        RuleFor(c => c.OrderIndex).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateTopicRequestValidator : AbstractValidator<CreateTopicRequest>
{
    public CreateTopicRequestValidator()
    {
        RuleFor(c => c.SectionId).GreaterThan(0);
        RuleFor(c => c.Title).NotEmpty().MaximumLength(300);
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase kebab-case (letters, digits, single dashes).");
        RuleFor(c => c.Content).NotEmpty();
        RuleFor(c => c.OrderIndex).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CompanyTags).MaximumLength(500)
            .When(c => !string.IsNullOrWhiteSpace(c.CompanyTags));
        RuleFor(c => c.Frequency).MaximumLength(32)
            .When(c => !string.IsNullOrWhiteSpace(c.Frequency));
        RuleFor(c => c.Difficulty).MaximumLength(32)
            .When(c => !string.IsNullOrWhiteSpace(c.Difficulty));
        RuleFor(c => c.ReadTimeMinutes).InclusiveBetween(1, 240)
            .When(c => c.ReadTimeMinutes.HasValue);
    }
}

public sealed class UpdateTopicRequestValidator : AbstractValidator<UpdateTopicRequest>
{
    public UpdateTopicRequestValidator()
    {
        RuleFor(c => c.Title).NotEmpty().MaximumLength(300);
        RuleFor(c => c.Slug).NotEmpty().MaximumLength(160)
            .Matches("^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be lowercase kebab-case (letters, digits, single dashes).");
        RuleFor(c => c.Content).NotEmpty();
        RuleFor(c => c.OrderIndex).GreaterThanOrEqualTo(0);
        RuleFor(c => c.CompanyTags).MaximumLength(500)
            .When(c => !string.IsNullOrWhiteSpace(c.CompanyTags));
        RuleFor(c => c.Frequency).MaximumLength(32)
            .When(c => !string.IsNullOrWhiteSpace(c.Frequency));
        RuleFor(c => c.Difficulty).MaximumLength(32)
            .When(c => !string.IsNullOrWhiteSpace(c.Difficulty));
        RuleFor(c => c.ReadTimeMinutes).InclusiveBetween(1, 240)
            .When(c => c.ReadTimeMinutes.HasValue);
    }
}

public sealed class ReorderRequestValidator : AbstractValidator<ReorderRequest>
{
    public ReorderRequestValidator()
    {
        RuleFor(c => c.OrderedIds).NotNull().Must(ids => ids.Count >= 1)
            .WithMessage("At least one id must be provided.");
    }
}

public sealed class UpdateLanguagePriceRequestValidator : AbstractValidator<UpdateLanguagePriceRequest>
{
    public UpdateLanguagePriceRequestValidator()
    {
        RuleFor(c => c.Price).InclusiveBetween(0m, 1_000_000m);
    }
}
