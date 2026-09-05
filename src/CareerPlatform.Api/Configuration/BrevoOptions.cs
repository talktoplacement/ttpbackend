using System.ComponentModel.DataAnnotations;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Configuration for the Brevo (formerly Sendinblue) transactional email adapter. Loaded from
/// `.env` / environment variables under the <c>Brevo:</c> section and validated at startup so
/// missing credentials halt the app before it tries to send an OTP (Req 15.2).
/// </summary>
public sealed class BrevoOptions
{
    public const string Section = "Brevo";

    /// <summary>Brevo API key (v3). Format: <c>xkeysib-...</c>.</summary>
    [Required, MinLength(20)]
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Verified sender email address on the Brevo account.</summary>
    [Required, EmailAddress]
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>Human-readable sender name shown in the recipient's inbox.</summary>
    [Required, MinLength(2)]
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Optional Brevo template id (numeric). When set, the API uses the templated payload
    /// (personalizations via <c>params</c>); when unset, a plain HTML fallback is sent.
    /// </summary>
    public int? OtpTemplateId { get; set; }

    /// <summary>Base URL for the Brevo API. Overridable for tests.</summary>
    public string ApiBaseUrl { get; set; } = "https://api.brevo.com/v3";
}
