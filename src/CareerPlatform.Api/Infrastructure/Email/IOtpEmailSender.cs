namespace CareerPlatform.Api.Infrastructure.Email;

/// <summary>
/// Sends a one-time-code email to the given recipient. Kept behind an interface so the delivery
/// mechanism (Brevo / SES / SMTP / test-double) is a swap-in concern.
/// </summary>
public interface IOtpEmailSender
{
    /// <summary>
    /// Delivers <paramref name="code"/> to <paramref name="recipientEmail"/> addressed to the
    /// display name <paramref name="recipientName"/>. Throws on delivery failure so the caller
    /// surfaces the error to the user instead of silently swallowing it.
    /// </summary>
    Task SendAsync(string recipientEmail, string recipientName, string code, TimeSpan ttl, CancellationToken cancellationToken);
}
