using CareerPlatform.Api.Features.Auth.Dto;
using CareerPlatform.Api.Features.Auth.Service;
using CareerPlatform.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CareerPlatform.Api.Features.Auth.Controller;

/// <summary>Anonymous authentication surface: login/register/password-reset flows.</summary>
[ApiController]
[Route("api/v1/auth")]  // canonical
[Produces("application/json")]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicy.Sensitive)]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _service;
    private readonly IAuthSessionCookie _sessionCookie;

    public AuthController(IAuthService service, IAuthSessionCookie sessionCookie)
    {
        _service = service;
        _sessionCookie = sessionCookie;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthTokenResponse>> Login(
        [FromBody] LoginRequest body, CancellationToken ct)
        => IssueSession(await _service.LoginAsync(body, ct));

    /// <summary>POST /logout — clears the session cookie. Anonymous so an expired session can still be cleared.</summary>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        _sessionCookie.Clear(HttpContext);
        return NoContent();
    }

    /// <summary>
    /// Attaches the session cookie to a successful token result.
    ///
    /// Shared by every endpoint that mints a token — login, registration verification, and password
    /// reset. Previously only login wrote the cookie, so a user who registered or reset their password
    /// received a token in the response body but no browser session, and the very next authenticated
    /// request 401'd.
    /// </summary>
    private ActionResult<AuthTokenResponse> IssueSession(Result<AuthTokenResponse> result)
    {
        if (result.IsSuccess)
        {
            _sessionCookie.Write(HttpContext, result.Value.AccessToken, result.Value.ExpiresInSeconds);
        }
        return result.ToActionResult();
    }

    [HttpPost("register/start")]
    public async Task<ActionResult<RegistrationInitiatedResponse>> StartRegistration(
        [FromBody] StartRegistrationRequest body, CancellationToken ct)
        => (await _service.StartRegistrationAsync(body, ct)).ToActionResult();

    [HttpPost("register/verify")]
    public async Task<ActionResult<AuthTokenResponse>> VerifyRegistration(
        [FromBody] VerifyRegistrationRequest body, CancellationToken ct)
        => IssueSession(await _service.VerifyRegistrationAsync(body, ct));

    [HttpPost("register/resend")]
    public async Task<ActionResult<RegistrationInitiatedResponse>> ResendOtp(
        [FromBody] ResendOtpRequest body, CancellationToken ct)
        => (await _service.ResendRegistrationOtpAsync(body.Email, ct)).ToActionResult();

    [HttpPost("password/forgot")]
    public async Task<ActionResult<RegistrationInitiatedResponse>> RequestPasswordReset(
        [FromBody] RequestPasswordResetRequest body, CancellationToken ct)
        => (await _service.RequestPasswordResetAsync(body.Email, ct)).ToActionResult();

    [HttpPost("password/reset")]
    public async Task<ActionResult<AuthTokenResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest body, CancellationToken ct)
        => IssueSession(await _service.ResetPasswordAsync(body, ct));
}
