using System.Security.Claims;

namespace CareerPlatform.Api.Infrastructure.Security;

/// <summary>
/// <see cref="ICurrentUser"/> implementation sourced from the ambient
/// <see cref="IHttpContextAccessor"/>. The principal id comes from
/// <see cref="ClaimTypes.NameIdentifier"/>; roles from the principal's role claims; permissions
/// from any <c>permission</c> claims (empty when none). When there is no authenticated principal
/// the id is <c>null</c> and both sets are empty (Req 10.3, 19.3, 19.4, 20).
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    /// <summary>The claim type carrying granular permissions, when present.</summary>
    public const string PermissionClaimType = "permission";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public string? UserId
    {
        get
        {
            var value = Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }

    /// <inheritdoc />
    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public IReadOnlySet<string> Roles =>
        Principal is null
            ? EmptySet
            : Principal.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlySet<string> Permissions =>
        Principal is null
            ? EmptySet
            : Principal.FindAll(PermissionClaimType)
                .Select(c => c.Value)
                .ToHashSet(StringComparer.Ordinal);

    private static readonly IReadOnlySet<string> EmptySet =
        new HashSet<string>(StringComparer.Ordinal);
}
