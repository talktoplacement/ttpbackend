using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CareerPlatform.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CareerPlatform.Api.Infrastructure.Security;

/// <summary>
/// HS256 <see cref="IJwtIssuer"/> matched to the validation configuration in
/// <c>AuthenticationRegistration</c>. Emits <c>sub</c> (user id), <c>email</c>, the operator-
/// configured role claim (default <c>role</c>), <c>iss</c>, <c>aud</c>, <c>iat</c>, <c>exp</c>.
/// Never touches secrets outside <see cref="JwtOptions"/>.
/// </summary>
public sealed class JwtIssuer : IJwtIssuer
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);
    private readonly JwtOptions _options;

    public JwtIssuer(IOptions<JwtOptions> options) => _options = options.Value;

    public string Issue(string userId, string email, string role, TimeSpan? ttl = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        var now = DateTime.UtcNow;
        var expires = now.Add(ttl ?? DefaultTtl);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new(_options.RoleClaim ?? "role", role ?? string.Empty),
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
