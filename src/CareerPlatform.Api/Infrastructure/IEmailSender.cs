namespace CareerPlatform.Api.Infrastructure;

/// <summary>
/// An outbound email abstraction. The default registration sends every message through the
/// Brevo transactional API (Req 17.1, 17.3) so OTP, password-reset, and admin/promotional
/// email all flow through a single provider.
/// </summary>
public interface IEmailSender
{
    /// <summary>Sends <paramref name="message"/> to its recipient.</summary>
    Task SendAsync(EmailMessage message, CancellationToken ct);
}

/// <summary>
/// A single outbound email: recipient address, subject, and HTML body.
/// </summary>
/// <param name="To">The recipient email address.</param>
/// <param name="Subject">The message subject line.</param>
/// <param name="HtmlBody">The message body, rendered as HTML.</param>
public sealed record EmailMessage(string To, string Subject, string HtmlBody);
