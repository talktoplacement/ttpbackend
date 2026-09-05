using CareerPlatform.Api.Features.Coupons.Dto;
using FluentValidation;

namespace CareerPlatform.Api.Features.Coupons.Validation;

/// <summary>Validation rules shared by create + update coupon flows.</summary>
internal static class CouponRules
{
    public static readonly string[] AllowedDiscountTypes = { "percent", "flat" };

    public static void Common<T>(this IRuleBuilder<T, string> code) =>
        code.NotEmpty().MaximumLength(64).Matches(@"^[A-Z0-9_-]+$")
            .WithMessage("Code must contain only uppercase letters, digits, hyphen, or underscore.");

    public static IRuleBuilderOptions<T, string> DiscountType<T>(
        this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .Must(t => AllowedDiscountTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"DiscountType must be one of: {string.Join(", ", AllowedDiscountTypes)}.");
}

public sealed class CreateCouponRequestValidator : AbstractValidator<CreateCouponRequest>
{
    public CreateCouponRequestValidator()
    {
        RuleFor(r => r.Code).Common();
        RuleFor(r => r.Description).MaximumLength(500);
        RuleFor(r => r.DiscountType).DiscountType();
        RuleFor(r => r.DiscountValue).GreaterThanOrEqualTo(0);
        RuleFor(r => r.DiscountValue).LessThanOrEqualTo(100)
            .When(r => string.Equals(r.DiscountType, "percent", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Percent discount cannot exceed 100.");
        RuleFor(r => r.MaxRedemptions).GreaterThanOrEqualTo(1).When(r => r.MaxRedemptions.HasValue);
        RuleFor(r => r).Must(r => !(r.StartsAtUtc.HasValue && r.ExpiresAtUtc.HasValue && r.StartsAtUtc >= r.ExpiresAtUtc))
            .WithMessage("StartsAtUtc must be earlier than ExpiresAtUtc.");
    }
}

public sealed class UpdateCouponRequestValidator : AbstractValidator<UpdateCouponRequest>
{
    public UpdateCouponRequestValidator()
    {
        RuleFor(r => r.Description).MaximumLength(500);
        RuleFor(r => r.DiscountType).DiscountType();
        RuleFor(r => r.DiscountValue).GreaterThanOrEqualTo(0);
        RuleFor(r => r.DiscountValue).LessThanOrEqualTo(100)
            .When(r => string.Equals(r.DiscountType, "percent", StringComparison.OrdinalIgnoreCase));
        RuleFor(r => r.MaxRedemptions).GreaterThanOrEqualTo(1).When(r => r.MaxRedemptions.HasValue);
        RuleFor(r => r).Must(r => !(r.StartsAtUtc.HasValue && r.ExpiresAtUtc.HasValue && r.StartsAtUtc >= r.ExpiresAtUtc))
            .WithMessage("StartsAtUtc must be earlier than ExpiresAtUtc.");
    }
}
