namespace CareerPlatform.Api.Common;

/// <summary>
/// A single page of results together with the paging metadata needed to render it.
/// A page beyond the last available page carries empty <see cref="Items"/> with the
/// requested page, the effective size, and the true total (Req 8.7).
/// </summary>
/// <typeparam name="T">The type of the items on the page.</typeparam>
/// <param name="Items">The items on this page; empty when the page is beyond the last.</param>
/// <param name="Page">The 1-based page number this result represents (&gt;= 1, Req 9.8).</param>
/// <param name="PageSize">The page size used to produce this result (&gt;= 1, Req 9.8).</param>
/// <param name="TotalCount">The total number of items across all pages (&gt;= 0, Req 8.1, 9.8).</param>
public sealed record PaginatedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    long TotalCount)
{
    /// <summary>
    /// Creates a <see cref="PaginatedResult{T}"/>, enforcing the metadata invariants:
    /// <paramref name="page"/> &gt;= 1, <paramref name="pageSize"/> &gt;= 1, and
    /// <paramref name="total"/> &gt;= 0 (Req 8.1, 9.8).
    /// </summary>
    public static PaginatedResult<T> Create(
        IReadOnlyList<T> items, int page, int pageSize, long total)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfNegative(total);

        return new PaginatedResult<T>(items, page, pageSize, total);
    }
}
