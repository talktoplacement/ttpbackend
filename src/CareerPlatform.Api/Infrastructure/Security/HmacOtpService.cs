using System.Security.Cryptography;
using System.Text;
using CareerPlatform.Api.Configuration;
using Microsoft.Extensions.Options;

namespace CareerPlatform.Api.Infrastructure.Security;

/// <summary>
/// HMAC-SHA256 <see cref="IOtpService"/>. Generates codes via <see cref="RandomNumberGenerator"/>
/// (cryptographic RNG, unbiased digit selection) and hashes with the operator-provided
/// <see cref="OtpOptions.HashKey"/> so a DB dump alone cannot regenerate valid codes.
/// </summary>
public sealed class HmacOtpService : IOtpService
{
    private readonly OtpOptions _options;

    public HmacOtpService(IOptions<OtpOptions> options) => _options = options.Value;

    public string Generate()
    {
        var length = _options.CodeLength;
        var digits = new char[length];
        // Reject-sampling from 0..9 avoids modulo bias when the byte range is not a multiple of 10.
        for (var i = 0; i < length; i++)
        {
            byte b;
            do { b = RandomNumberGenerator.GetBytes(1)[0]; } while (b >= 250);
            digits[i] = (char)('0' + b % 10);
        }
        return new string(digits);
    }

    public string Hash(string code)
    {
        ArgumentException.ThrowIfNullOrEmpty(code);
        var keyBytes = Encoding.UTF8.GetBytes(_options.HashKey);
        using var hmac = new HMACSHA256(keyBytes);
        var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
