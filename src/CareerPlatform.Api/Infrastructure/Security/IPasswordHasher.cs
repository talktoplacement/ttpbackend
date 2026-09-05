namespace CareerPlatform.Api.Infrastructure.Security;

/// <summary>
/// Password hashing contract used by the auth slices. Kept behind an interface so the underlying
/// algorithm (currently BCrypt) can be swapped without touching handlers.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Hashes a plaintext password using a cryptographically random per-hash salt.</summary>
    string Hash(string password);

    /// <summary>Constant-time verify of <paramref name="password"/> against <paramref name="hash"/>.</summary>
    bool Verify(string password, string hash);
}
