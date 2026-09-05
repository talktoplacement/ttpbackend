namespace CareerPlatform.Api.Infrastructure.Search;

/// <summary>
/// Minimal placeholder <see cref="ISearchService"/> that returns an empty page. A real search
/// backend can be substituted with a registration-only change (Req 17.1, 17.2, 17.5).
/// </summary>
public sealed class SearchService : ISearchService
{
    /// <inheritdoc />
    public Task<PaginatedResult<TDoc>> SearchAsync<TDoc>(
        string query, PaginationRequest page, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(page);
        ct.ThrowIfCancellationRequested();

        var empty = PaginatedResult<TDoc>.Create(
            Array.Empty<TDoc>(), page.EffectivePage, page.EffectivePageSize, 0);

        return Task.FromResult(empty);
    }
}
