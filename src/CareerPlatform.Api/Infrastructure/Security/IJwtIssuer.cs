namespace CareerPlatform.Api.Infrastructure.Security;

/// <summary>
/// Issues signed JWTs for authenticated principals. The token carries the standard set of claims
/// (<c>sub</c>, <c>email</c>, <c>role</c>) plus <c>iss</c>/<c>aud</c>/<c>exp</c>. The signing key
/// and issuer come from <see cref="Configuration.JwtOptions"/> so the same tokens validate
/// against the existing <see cref="Microsoft.AspNetCore.Authentication.JwtBearer"/> middleware.
/// </summary>
public interface IJwtIssuer
{
    /// <summary>Issues a token for the given user; <paramref name="ttl"/> defaults to 60 minutes.</summary>
    string Issue(string userId, string email, string role, TimeSpan? ttl = null);
}
