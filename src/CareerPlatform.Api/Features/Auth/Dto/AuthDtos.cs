namespace CareerPlatform.Api.Features.Auth.Dto;

/// <summary>Response for successful login and completed registration.</summary>
public sealed record AuthTokenResponse(string AccessToken, int ExpiresInSeconds, AuthPrincipal Principal);

/// <summary>Minimal principal projection returned alongside a fresh access token.</summary>
public sealed record AuthPrincipal(string Id, string Email, string FullName, string Role);

/// <summary>Response for register-start / resend-otp / forgot-password. Never carries the OTP.</summary>
public sealed record RegistrationInitiatedResponse(
    string Email, int ExpirySeconds, int ResendCooldownSeconds, int MaxAttempts);

public sealed record LoginRequest(string Email, string Password);

public sealed record StartRegistrationRequest(
    string Email, string FullName, string MobileNumber,
    string? YearsOfExperience, string IntendedRole, string Password);

public sealed record VerifyRegistrationRequest(string Email, string Code);

public sealed record ResendOtpRequest(string Email);

public sealed record RequestPasswordResetRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Code, string NewPassword);
