using System.Security.Claims;
using System.Text;
using CareerPlatform.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Encapsulates the Supabase JWT bearer authentication wiring and the role-claim normalization
/// that were previously inline in <c>Program.cs</c> (Req 19, 20).
///
/// The token-validation parameters are configured via a post-configure that consumes
/// <see cref="IOptions{JwtOptions}"/> rather than raw configuration reads. Because
/// <c>JwtOptions.Secret</c> is <c>[Required]</c> with <c>ValidateOnStart</c>, a missing secret
/// halts startup before this runs, so the signing key is always present here (Req 15.3).
/// </summary>
public static class AuthenticationRegistration
{
    /// <summary>
    /// Registers Supabase JWT bearer authentication (issuer/audience/signing-key validation,
    /// <c>ValidateLifetime</c>, clock skew ≤ 60s, <see cref="ClaimTypes.Role"/> role mapping and
    /// <see cref="ClaimTypes.NameIdentifier"/> name mapping) plus the
    /// <see cref="SupabaseRoleClaimsTransformation"/> that normalizes the Supabase role claim to
    /// <see cref="ClaimTypes.Role"/> so role-based authorization resolves regardless of whether
    /// the role is emitted top-level or in user/app metadata (Req 19.1-19.5, 20.2-20.4).
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Configure the token-validation parameters from the validated JwtOptions. Kept as a
        // post-configure so it runs after options binding/validation (Req 15.3, 19.1).
        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptionsAccessor) =>
            {
                var jwt = jwtOptionsAccessor.Value;
                var keyBytes = Encoding.UTF8.GetBytes(jwt.Secret);

                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ClockSkew = TimeSpan.FromSeconds(60),
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier,
                };

                // Accept the token from the HttpOnly session cookie when no Authorization header
                // is supplied. This lets the browser authenticate without JavaScript ever holding
                // the JWT (XSS cannot read an HttpOnly cookie), while programmatic/bearer clients
                // continue to work unchanged.
                bearer.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        if (string.IsNullOrEmpty(context.Token)
                            && context.Request.Cookies.TryGetValue(AuthCookie.Name, out var cookieToken)
                            && !string.IsNullOrEmpty(cookieToken))
                        {
                            context.Token = cookieToken;
                        }

                        return Task.CompletedTask;
                    },
                };
            });

        // Normalize the Supabase role claim to ClaimTypes.Role so role-based policies work
        // whether the role is emitted top-level or in user/app metadata (Req 19.2, 20.2).
        services.AddTransient<IClaimsTransformation, SupabaseRoleClaimsTransformation>();

        return services;
    }
}
