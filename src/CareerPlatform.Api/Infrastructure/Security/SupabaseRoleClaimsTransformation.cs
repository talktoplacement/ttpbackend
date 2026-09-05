using System.Security.Claims;
using CareerPlatform.Api.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CareerPlatform.Api.Infrastructure.Security;

/// <summary>
/// Normalizes the Supabase-issued application role claim to ASP.NET Core's
/// <see cref="ClaimTypes.Role"/> so that <c>[Authorize(Roles = "Admin")]</c> and the
/// <c>Admin</c> authorization policy resolve correctly (Req 19, 20).
///
/// Supabase can emit the role either as a top-level claim (name configurable via
/// <c>Jwt:RoleClaim</c>, default <c>role</c>) or nested inside <c>user_metadata</c>/
/// <c>app_metadata</c> (flattened by the JWT handler into <c>user_role</c>/<c>app_role</c>
/// claims). This transformation copies any value found under the configured claim name (or the
/// well-known metadata claims) into a <see cref="ClaimTypes.Role"/> claim when one is not already
/// present. It is idempotent: repeat invocations on the same principal are a no-op once a
/// <see cref="ClaimTypes.Role"/> claim exists (Req 19.2).
/// </summary>
public sealed class SupabaseRoleClaimsTransformation : IClaimsTransformation
{
    private static readonly string[] MetadataRoleClaimNames =
    {
        "user_role",
        "app_role",
    };

    private readonly string _roleClaimName;

    public SupabaseRoleClaimsTransformation(IOptions<JwtOptions> jwtOptions)
    {
        var configured = jwtOptions.Value.RoleClaim;
        _roleClaimName = string.IsNullOrWhiteSpace(configured) ? "role" : configured;
    }

    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return Task.FromResult(principal);
        }

        // Nothing to do if a standard role claim is already present (idempotent for repeat
        // invocations, and a no-op when the token already carries ClaimTypes.Role directly).
        if (identity.HasClaim(c => c.Type == ClaimTypes.Role))
        {
            return Task.FromResult(principal);
        }

        var roleClaim = principal.FindFirst(_roleClaimName)
            ?? MetadataRoleClaimNames
                .Select(name => principal.FindFirst(name))
                .FirstOrDefault(c => c is not null);

        if (roleClaim is not null && !string.IsNullOrWhiteSpace(roleClaim.Value))
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
        }

        return Task.FromResult(principal);
    }
}
