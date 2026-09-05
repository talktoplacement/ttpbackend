namespace CareerPlatform.Api.Common;

/// <summary>
/// A requested page number and page size, either of which may be omitted. The effective
/// values apply defaults when omitted and clamp the page size to <see cref="MaxPageSize"/>
/// (Req 8.2, 8.3, 8.4). Rejection of sub-minimum values (page &lt; 1, size &lt; 1) is a
/// validator concern handled by <c>PaginationValidator</c>, not here.
/// </summary>
/// <param name="Page">The requested 1-based page number, or <c>null</c> to use the default.</param>
/// <param name="PageSize">The requested page size, or <c>null</c> to use the default.</param>
public sealed record PaginationRequest(int? Page, int? PageSize)
{
    /// <summary>The page number applied when none is requested.</summary>
    public const int DefaultPage = 1;

    /// <summary>The page size applied when none is requested.</summary>
    public const int DefaultPageSize = 20;

    /// <summary>The maximum page size; larger requested sizes are clamped to this value.</summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// The effective page number: the requested page, or <see cref="DefaultPage"/> when omitted
    /// (Req 8.2).
    /// </summary>
    public int EffectivePage => Page ?? DefaultPage;

    /// <summary>
    /// The effective page size: the requested size (or <see cref="DefaultPageSize"/> when omitted),
    /// clamped to at most <see cref="MaxPageSize"/> (Req 8.3, 8.4).
    /// </summary>
    public int EffectivePageSize => Math.Min(PageSize ?? DefaultPageSize, MaxPageSize);
}
