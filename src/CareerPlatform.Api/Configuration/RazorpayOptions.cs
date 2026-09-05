using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Strongly-typed binding for the <c>Razorpay</c> configuration section (Req 15.1).
/// The <see cref="KeySecret"/> is required for signature verification; a missing
/// value fails startup validation (Req 15.2, 15.3).
/// </summary>
public sealed class RazorpayOptions
{
    public const string Section = "Razorpay";

    /// <summary>Public Razorpay key id.</summary>
    public string? KeyId { get; init; }

    /// <summary>Razorpay key secret. Required — startup fails fast when absent.</summary>
    [Required(AllowEmptyStrings = false)]
    public string KeySecret { get; init; } = string.Empty;
}
