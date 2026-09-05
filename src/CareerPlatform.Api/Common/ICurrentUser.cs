namespace CareerPlatform.Api.Common;

/// <summary>
/// Ambient accessor for the authenticated principal of the current request. The concrete
/// <c>HttpCurrentUser</c> implementation (populated from <c>IHttpContextAccessor</c>) is added in
/// task 8.1; this interface is consumed by handlers and the AuditableEntityInterceptor (Req 10.3,
/// 19.3, 19.4, 20).
/// </summary>
public interface ICurrentUser
{
    /// <summary>The principal id, or <c>null</c> when the request is unauthenticated (Req 19.4).</summary>
    string? UserId { get; }

    /// <summary>Whether the current request carries an authenticated principal.</summary>
    bool IsAuthenticated { get; }

    /// <summary>The principal's roles; empty when unauthenticated (Req 19.4).</summary>
    IReadOnlySet<string> Roles { get; }

    /// <summary>The principal's permissions; empty when unauthenticated (Req 19.4).</summary>
    IReadOnlySet<string> Permissions { get; }
}
