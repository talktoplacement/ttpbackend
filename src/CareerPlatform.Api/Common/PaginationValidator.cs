using System.Linq.Expressions;
using FluentValidation;

namespace CareerPlatform.Api.Common;

/// <summary>
/// Shared, reusable FluentValidation rules for pagination inputs. A requested page number
/// less than 1 is rejected with a field error naming the page-number field, and a requested
/// page size less than 1 is rejected with a field error naming the page-size field (Req 8.5,
/// 8.6). Omitted (null) values are allowed: they fall back to the defaults applied by
/// <see cref="PaginationRequest"/>, so they raise no error.
/// </summary>
public static class PaginationRules
{
    /// <summary>The minimum accepted page number and page size.</summary>
    public const int Minimum = 1;

    /// <summary>
    /// Applies the shared pagination rules to a validator whose request embeds a nullable
    /// page-number and page-size field. Query validators that carry their own pagination
    /// properties reuse this so the rejection logic lives in exactly one place. Only non-null
    /// values are checked, so omitted values (which use defaults) never fail.
    /// </summary>
    /// <typeparam name="T">The request type being validated.</typeparam>
    /// <param name="validator">The validator to extend.</param>
    /// <param name="page">Selector for the nullable page-number field.</param>
    /// <param name="pageSize">Selector for the nullable page-size field.</param>
    public static void ApplyPaginationRules<T>(
        this AbstractValidator<T> validator,
        Expression<Func<T, int?>> page,
        Expression<Func<T, int?>> pageSize)
    {
        var pageAccessor = page.Compile();
        var pageSizeAccessor = pageSize.Compile();

        // Page < 1 → field error on the page-number field (Req 8.5). Null is allowed.
        validator.RuleFor(page)
            .GreaterThanOrEqualTo(Minimum)
            .When(instance => pageAccessor(instance) is not null);

        // PageSize < 1 → field error on the page-size field (Req 8.6). Null is allowed.
        validator.RuleFor(pageSize)
            .GreaterThanOrEqualTo(Minimum)
            .When(instance => pageSizeAccessor(instance) is not null);
    }
}

/// <summary>
/// Standalone validator for a <see cref="PaginationRequest"/>. Rejects <c>Page &lt; 1</c> and
/// <c>PageSize &lt; 1</c> with named field errors (Req 8.5, 8.6). Query validators whose request
/// embeds pagination fields under different property names should instead reuse
/// <see cref="PaginationRules.ApplyPaginationRules{T}"/> to mix the same rules in.
/// </summary>
public sealed class PaginationRequestValidator : AbstractValidator<PaginationRequest>
{
    public PaginationRequestValidator()
    {
        this.ApplyPaginationRules(x => x.Page, x => x.PageSize);
    }
}
