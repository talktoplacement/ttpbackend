namespace CareerPlatform.Api.Infrastructure.Security;

/// <summary>
/// OTP generation + verification abstraction. Codes are never stored in plaintext — only the
/// HMAC-SHA256 hash is persisted, so a DB dump does not leak valid codes.
/// </summary>
public interface IOtpService
{
    /// <summary>Generates a cryptographically random numeric code of the configured length.</summary>
    string Generate();

    /// <summary>Deterministic HMAC-SHA256 hash of the code, hex-lowercased. Suitable for equality checks.</summary>
    string Hash(string code);
}
