using System.Security.Claims;
using CareerPlatform.Api.Features.SubscriptionPlans.Lifecycle;
using CareerPlatform.Api.Features.Users.Domain;
using CareerPlatform.Api.Features.Users.Dto;
using CareerPlatform.Api.Infrastructure.Persistence;
using CareerPlatform.Api.Infrastructure.Persistence.Seed;
using CareerPlatform.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace CareerPlatform.Api.Features.Users.Service;

/// <summary>Self-service profile workflow. Ports the 4 legacy MediatR handlers verbatim.</summary>
internal sealed class UserService : IUserService
{
    private static readonly Error CurrentPasswordInvalid = Error.Unauthorized(
        "Auth.CurrentPasswordInvalid", "The current password is incorrect.");

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IHttpContextAccessor _httpContext;

    public UserService(
        AppDbContext db, ICurrentUser currentUser,
        IPasswordHasher passwordHasher, IHttpContextAccessor httpContext)
    {
        _db = db;
        _currentUser = currentUser;
        _passwordHasher = passwordHasher;
        _httpContext = httpContext;
    }

    public async Task<Result<MyProfileResponse>> GetMineAsync(CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<MyProfileResponse>(Error.Unauthorized(
                "Profile.Unauthorized", "An authenticated user is required to read the profile."));
        }
        var profile = await _db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (profile is null)
        {
            return Result.Failure<MyProfileResponse>(Error.NotFound(
                "Profile.NotFound", "No profile exists for the authenticated user."));
        }

        // Entitlement is derived from the current active subscription — the authoritative source —
        // and takes precedence over the denormalized PlanName cache. The client gates paid features
        // on the returned IsPro flag, so this is the single place entitlement is decided.
        var effectivePlan = await EntitlementDeriver.DeriveEffectivePlanAsync(
            _db, userId, DateTime.UtcNow, ct);

        return Result.Success(MyProfileResponse.From(profile, effectivePlan));
    }

    public async Task<Result<MyProfileResponse>> UpdateMineAsync(UpdateMyProfileRequest r, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<MyProfileResponse>(Error.Unauthorized(
                "Profile.Unauthorized", "An authenticated user is required to update the profile."));
        }
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (profile is null)
        {
            return Result.Failure<MyProfileResponse>(Error.NotFound(
                "Profile.NotFound", "No profile exists for the authenticated user."));
        }
        profile.FullName = r.FullName;
        profile.Phone = Normalize(r.Phone);
        profile.Designation = Normalize(r.Designation);
        profile.Department = Normalize(r.Department);
        await _db.SaveChangesAsync(ct);
        return Result.Success(MyProfileResponse.From(profile));
    }

    public async Task<Result> ChangeMyPasswordAsync(string currentPassword, string newPassword, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure(Error.Unauthorized(
                "Auth.Unauthorized", "An authenticated user is required to change the password."));
        }
        var user = await _db.UserProfiles.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return Result.Failure(CurrentPasswordInvalid);
        }
        if (!_passwordHasher.Verify(currentPassword, user.PasswordHash))
        {
            return Result.Failure(CurrentPasswordInvalid);
        }
        user.PasswordHash = _passwordHasher.Hash(newPassword);
        user.PasswordResetOtpHash = null;
        user.PasswordResetOtpExpiresAt = null;
        user.PasswordResetOtpAttemptsRemaining = 0;
        user.PasswordResetOtpLastSentAt = null;
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<MyProfileResponse>> SyncAsync(string? displayName, CancellationToken ct)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return Result.Failure<MyProfileResponse>(Error.Unauthorized(
                "Profile.Unauthorized", "An authenticated user is required to sync the profile."));
        }

        var jwt = _httpContext.HttpContext?.User;
        var email = jwt?.FindFirstValue(ClaimTypes.Email)
                 ?? jwt?.FindFirstValue("email")
                 ?? string.Empty;
        var jwtRole = jwt?.FindFirstValue(ClaimTypes.Role) ?? Roles.Student;
        var name = displayName
            ?? jwt?.FindFirstValue("name")
            ?? jwt?.FindFirstValue("full_name")
            ?? DeriveNameFromEmail(email);

        var profile = await _db.UserProfiles.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (profile is null)
        {
            profile = new UserProfile
            {
                Email = email,
                FullName = name,
                Role = jwtRole,
                PlanName = "Free",
                CreatedAt = DateTime.UtcNow,
            };
            var entry = _db.UserProfiles.Add(profile);
            entry.Property(u => u.Id).CurrentValue = userId;
        }
        else
        {
            if (!string.IsNullOrEmpty(email) &&
                !string.Equals(profile.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                profile.Email = email;
            }
            if (!string.IsNullOrEmpty(jwtRole) &&
                !string.Equals(profile.Role, jwtRole, StringComparison.Ordinal))
            {
                profile.Role = jwtRole;
            }
        }
        await _db.SaveChangesAsync(ct);
        return Result.Success(MyProfileResponse.From(profile));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string DeriveNameFromEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "User";
        var local = email.Split('@')[0];
        var parts = local.Split(new[] { '.', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }
}
