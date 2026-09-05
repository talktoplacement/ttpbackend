using CareerPlatform.Api.Features.Auth.Dto;

namespace CareerPlatform.Api.Features.Auth.Service;

public interface IAuthService
{
    Task<Result<AuthTokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct);
    Task<Result<RegistrationInitiatedResponse>> StartRegistrationAsync(StartRegistrationRequest request, CancellationToken ct);
    Task<Result<AuthTokenResponse>> VerifyRegistrationAsync(VerifyRegistrationRequest request, CancellationToken ct);
    Task<Result<RegistrationInitiatedResponse>> ResendRegistrationOtpAsync(string email, CancellationToken ct);
    Task<Result<RegistrationInitiatedResponse>> RequestPasswordResetAsync(string email, CancellationToken ct);
    Task<Result<AuthTokenResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
}
