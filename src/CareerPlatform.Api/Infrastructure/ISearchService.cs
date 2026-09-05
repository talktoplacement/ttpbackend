namespace CareerPlatform.Api.Infrastructure;

/// <summary>
/// A full-text/document search abstraction returning a page of matching documents. The
/// default registration is a minimal placeholder; a real search backend can be substituted
/// with a registration-only change (Req 17.1, 17.3).
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches for documents of type <typeparamref name="TDoc"/> matching
    /// <paramref name="query"/>, returning the requested page.
    /// </summary>
    Task<PaginatedResult<TDoc>> SearchAsync<TDoc>(
        string query, PaginationRequest page, CancellationToken ct);
}
