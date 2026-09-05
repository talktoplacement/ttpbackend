using CareerPlatform.Api.Features.Products.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Products.Validation;

internal static class ProductRules
{
    public static readonly string[] AllowedTypes = { "one-time", "add-on", "consultation" };

    public static IRuleBuilderOptions<T, string> ProductType<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"ProductType must be one of: {string.Join(", ", AllowedTypes)}.");
}

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(r => r.Code).NotEmpty().MaximumLength(64)
            .Matches(@"^[A-Z0-9_-]+$")
            .WithMessage("Code must contain only uppercase letters, digits, hyphen, or underscore.");
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Description).MaximumLength(2000);
        RuleFor(r => r.ProductType).ProductType();
        RuleFor(r => r.Price).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Currency).NotEmpty().Length(3);
    }
}

public sealed class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().MaximumLength(200);
        RuleFor(r => r.Description).MaximumLength(2000);
        RuleFor(r => r.ProductType).ProductType();
        RuleFor(r => r.Price).GreaterThanOrEqualTo(0);
        RuleFor(r => r.Currency).NotEmpty().Length(3);
    }
}
