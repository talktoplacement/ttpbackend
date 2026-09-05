using BCrypt.Net;

namespace CareerPlatform.Api.Infrastructure.Security;

/// <summary>
/// BCrypt-based <see cref="IPasswordHasher"/> using work factor 12 — the current industry default
/// for interactive login (single-hash time ≈ 250ms on modern hardware, which is what deters
/// brute-force while remaining tolerable for users). BCrypt.Net-Next embeds the salt and cost
/// factor in the returned string, so verification needs only the stored hash.
/// </summary>
public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(hash)) return false;
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (SaltParseException)
        {
            // Malformed stored hash — treat as verification failure rather than throw.
            return false;
        }
    }
}
