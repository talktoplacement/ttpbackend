using CareerPlatform.Api.Configuration;
using CareerPlatform.Api.Features.Auth.Dto;
using CareerPlatform.Api.Features.Users.Domain;
using CareerPlatform.Api.Infrastructure.Email;
using CareerPlatform.Api.Infrastructure.Persistence;
using CareerPlatform.Api.Infrastructure.Persistence.Seed;
using CareerPlatform.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Auth.Service;

/// <summary>
/// Auth workflow. Ports the 6 legacy MediatR handlers verbatim (login, register start/verify/
/// resend, password forgot/reset). Sensitive-info flows (email enumeration, timing attacks) all
/// preserve their uniform-response guarantees.
/// </summary>
internal sealed class AuthService : IAuthService
{
    private static readonly Error InvalidCredentials =
        Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password.");
    private static readonly Error GenericResetFailure = Error.Validation(
        "Auth.PasswordResetInvalid",
        "The verification code is invalid or has expired. Please request a new one.");

    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpService _otpService;
    private readonly IOtpEmailSender _emailSender;
    private readonly IJwtIssuer _jwtIssuer;
    private readonly OtpOptions _otpOptions;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IOtpService otpService,
        IOtpEmailSender emailSender,
        IJwtIssuer jwtIssuer,
        IOptions<OtpOptions> otpOptions,
        ILogger<AuthService> logger)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _otpService = otpService;
        _emailSender = emailSender;
        _jwtIssuer = jwtIssuer;
        _otpOptions = otpOptions.Value;
        _logger = logger;
    }

    public async Task<Result<AuthTokenResponse>> LoginAsync(LoginRequest r, CancellationToken ct)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        var user = await _db.UserProfiles.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return Result.Failure<AuthTokenResponse>(InvalidCredentials);
        }
        if (!_passwordHasher.Verify(r.Password, user.PasswordHash))
        {
            return Result.Failure<AuthTokenResponse>(InvalidCredentials);
        }
        return Result.Success(IssueToken(user));
    }

    public async Task<Result<RegistrationInitiatedResponse>> StartRegistrationAsync(StartRegistrationRequest r, CancellationToken ct)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        var role = NormalizeRole(r.IntendedRole);

        var taken = await _db.UserProfiles.AnyAsync(u => u.Email == email, ct);
        if (taken)
        {
            return Result.Failure<RegistrationInitiatedResponse>(Error.Conflict(
                "Auth.EmailTaken", "An account with this email already exists."));
        }

        var plainCode = _otpService.Generate();
        var now = DateTime.UtcNow;
        var ttl = TimeSpan.FromSeconds(_otpOptions.ExpirySeconds);
        await _emailSender.SendAsync(email, r.FullName, plainCode, ttl, ct);

        var pending = await _db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);
        if (pending is null)
        {
            pending = new PendingRegistration { Email = email, CreatedAt = now };
            _db.PendingRegistrations.Add(pending);
        }
        pending.FullName = r.FullName.Trim();
        pending.MobileNumber = r.MobileNumber.Trim();
        pending.YearsOfExperience = string.IsNullOrWhiteSpace(r.YearsOfExperience) ? null : r.YearsOfExperience.Trim();
        pending.IntendedRole = role;
        pending.PasswordHash = _passwordHasher.Hash(r.Password);
        pending.OtpHash = _otpService.Hash(plainCode);
        pending.OtpExpiresAt = now.Add(ttl);
        pending.OtpAttemptsRemaining = _otpOptions.MaxAttempts;
        pending.OtpLastSentAt = now;

        await _db.SaveChangesAsync(ct);
        return Result.Success(new RegistrationInitiatedResponse(
            email, _otpOptions.ExpirySeconds, _otpOptions.ResendCooldownSeconds, _otpOptions.MaxAttempts));
    }

    public async Task<Result<AuthTokenResponse>> VerifyRegistrationAsync(VerifyRegistrationRequest r, CancellationToken ct)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        var pending = await _db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);
        if (pending is null)
        {
            return Result.Failure<AuthTokenResponse>(Error.NotFound(
                "Auth.PendingNotFound", "No pending registration exists for this email. Please start again."));
        }
        if (pending.OtpAttemptsRemaining <= 0)
        {
            return Result.Failure<AuthTokenResponse>(Error.Validation(
                "Auth.OtpAttemptsExceeded", "Too many wrong attempts. Please request a new code."));
        }
        if (DateTime.UtcNow > pending.OtpExpiresAt)
        {
            return Result.Failure<AuthTokenResponse>(Error.Validation(
                "Auth.OtpExpired", "This verification code has expired. Please request a new one."));
        }

        var providedHash = _otpService.Hash(r.Code);
        if (!CryptographicEquals(providedHash, pending.OtpHash))
        {
            pending.OtpAttemptsRemaining--;
            await _db.SaveChangesAsync(ct);
            return Result.Failure<AuthTokenResponse>(Error.Validation(
                "Auth.OtpInvalid", $"Invalid verification code. {pending.OtpAttemptsRemaining} attempt(s) remaining."));
        }

        var alreadyRegistered = await _db.UserProfiles.AnyAsync(u => u.Email == email, ct);
        if (alreadyRegistered)
        {
            _db.PendingRegistrations.Remove(pending);
            await _db.SaveChangesAsync(ct);
            return Result.Failure<AuthTokenResponse>(Error.Conflict(
                "Auth.EmailTaken", "An account with this email already exists."));
        }

        var role = NormalizeRoleForStorage(pending.IntendedRole);
        var profile = new UserProfile
        {
            Email = pending.Email,
            FullName = pending.FullName,
            Phone = pending.MobileNumber,
            YearsOfExperience = pending.YearsOfExperience,
            Role = role,
            PlanName = "Free",
            PasswordHash = pending.PasswordHash,
            CreatedAt = DateTime.UtcNow,
        };
        var entry = _db.UserProfiles.Add(profile);
        entry.Property(u => u.Id).CurrentValue = Guid.NewGuid().ToString();
        _db.PendingRegistrations.Remove(pending);
        await _db.SaveChangesAsync(ct);

        return Result.Success(IssueToken(profile));
    }

    public async Task<Result<RegistrationInitiatedResponse>> ResendRegistrationOtpAsync(string emailRaw, CancellationToken ct)
    {
        var email = emailRaw.Trim().ToLowerInvariant();
        var pending = await _db.PendingRegistrations.FirstOrDefaultAsync(p => p.Email == email, ct);
        if (pending is null)
        {
            return Result.Failure<RegistrationInitiatedResponse>(Error.NotFound(
                "Auth.PendingNotFound", "No pending registration exists for this email. Please start again."));
        }
        var now = DateTime.UtcNow;
        var elapsed = now - pending.OtpLastSentAt;
        var cooldown = TimeSpan.FromSeconds(_otpOptions.ResendCooldownSeconds);
        if (elapsed < cooldown)
        {
            var wait = (int)Math.Ceiling((cooldown - elapsed).TotalSeconds);
            return Result.Failure<RegistrationInitiatedResponse>(Error.Validation(
                "Auth.OtpResendTooSoon", $"Please wait {wait} more second(s) before requesting a new code."));
        }

        var plainCode = _otpService.Generate();
        var ttl = TimeSpan.FromSeconds(_otpOptions.ExpirySeconds);
        await _emailSender.SendAsync(email, pending.FullName, plainCode, ttl, ct);

        pending.OtpHash = _otpService.Hash(plainCode);
        pending.OtpExpiresAt = now.Add(ttl);
        pending.OtpAttemptsRemaining = _otpOptions.MaxAttempts;
        pending.OtpLastSentAt = now;
        await _db.SaveChangesAsync(ct);

        return Result.Success(new RegistrationInitiatedResponse(
            email, _otpOptions.ExpirySeconds, _otpOptions.ResendCooldownSeconds, _otpOptions.MaxAttempts));
    }

    public async Task<Result<RegistrationInitiatedResponse>> RequestPasswordResetAsync(string emailRaw, CancellationToken ct)
    {
        // Anti-enumeration contract: the response body and status are IDENTICAL regardless of
        //   (a) whether the email is registered, or
        //   (b) whether the caller is inside the resend cooldown window.
        // A previous version returned Result.Failure("Auth.PasswordResetTooSoon", waitSeconds) when
        // the cooldown was hit, which let an attacker distinguish "email exists + hot" from
        // "unknown email" by inspecting the response shape — defeating the anti-enumeration
        // intent. We now silently no-op on cooldown/unknown and always return the same success
        // payload. Rate limiting (RateLimitPolicy.Sensitive on the controller) prevents abuse.
        var email = emailRaw.Trim().ToLowerInvariant();
        var response = new RegistrationInitiatedResponse(
            email, _otpOptions.ExpirySeconds, _otpOptions.ResendCooldownSeconds, _otpOptions.MaxAttempts);

        var user = await _db.UserProfiles.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null)
        {
            // Do NOT log the plain email — that would turn the log stream into an enumeration
            // oracle for anyone with log access. Log only that a reset was requested.
            _logger.LogInformation("Password-reset requested for an unknown address (suppressed).");
            return Result.Success(response);
        }

        var now = DateTime.UtcNow;
        if (user.PasswordResetOtpLastSentAt is DateTime last)
        {
            var elapsed = now - last;
            var cooldown = TimeSpan.FromSeconds(_otpOptions.ResendCooldownSeconds);
            if (elapsed < cooldown)
            {
                // Silent no-op — SAME response shape as the unknown-email branch above so the
                // two cases are indistinguishable to the caller.
                _logger.LogInformation(
                    "Password-reset resend within cooldown suppressed for {UserId}.", user.Id);
                return Result.Success(response);
            }
        }

        var plainCode = _otpService.Generate();
        var ttl = TimeSpan.FromSeconds(_otpOptions.ExpirySeconds);
        await _emailSender.SendAsync(email, user.FullName, plainCode, ttl, ct);

        user.PasswordResetOtpHash = _otpService.Hash(plainCode);
        user.PasswordResetOtpExpiresAt = now.Add(ttl);
        user.PasswordResetOtpAttemptsRemaining = _otpOptions.MaxAttempts;
        user.PasswordResetOtpLastSentAt = now;
        await _db.SaveChangesAsync(ct);

        return Result.Success(response);
    }

    public async Task<Result<AuthTokenResponse>> ResetPasswordAsync(ResetPasswordRequest r, CancellationToken ct)
    {
        var email = r.Email.Trim().ToLowerInvariant();
        var user = await _db.UserProfiles.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null
            || string.IsNullOrEmpty(user.PasswordResetOtpHash)
            || user.PasswordResetOtpExpiresAt is null
            || user.PasswordResetOtpAttemptsRemaining <= 0
            || DateTime.UtcNow > user.PasswordResetOtpExpiresAt.Value)
        {
            return Result.Failure<AuthTokenResponse>(GenericResetFailure);
        }

        var providedHash = _otpService.Hash(r.Code);
        if (!CryptographicEquals(providedHash, user.PasswordResetOtpHash))
        {
            user.PasswordResetOtpAttemptsRemaining--;
            await _db.SaveChangesAsync(ct);
            return Result.Failure<AuthTokenResponse>(GenericResetFailure);
        }

        user.PasswordHash = _passwordHasher.Hash(r.NewPassword);
        user.PasswordResetOtpHash = null;
        user.PasswordResetOtpExpiresAt = null;
        user.PasswordResetOtpAttemptsRemaining = 0;
        user.PasswordResetOtpLastSentAt = null;
        await _db.SaveChangesAsync(ct);

        return Result.Success(IssueToken(user));
    }

    private AuthTokenResponse IssueToken(UserProfile user)
    {
        var token = _jwtIssuer.Issue(user.Id, user.Email, user.Role);
        return new AuthTokenResponse(
            token, (int)TimeSpan.FromHours(1).TotalSeconds,
            new AuthPrincipal(user.Id, user.Email, user.FullName, user.Role));
    }

    private static bool CryptographicEquals(string a, string b)
    {
        if (a.Length != b.Length) return false;
        var diff = 0;
        for (var i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }

    private static string NormalizeRole(string role) =>
        char.ToUpperInvariant(role[0]) + role[1..].ToLowerInvariant();

    private static string NormalizeRoleForStorage(string role) =>
        role.Equals("Mentor", StringComparison.OrdinalIgnoreCase) ? "Mentor" : Roles.Student;
}
