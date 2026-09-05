using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CareerPlatform.Api.Configuration;
using Microsoft.Extensions.Options;

namespace CareerPlatform.Api.Infrastructure.Email;

/// <summary>
/// <see cref="IEmailSender"/> implementation that sends every outbound message through the
/// Brevo (v3) transactional email API — the same channel the OTP sender uses. Consolidating
/// on one provider means one API key to rotate, one dashboard for deliverability, and one
/// sender-verification surface.
///
/// Uses <see cref="IHttpClientFactory"/> so timeouts, DNS caching, and connection pooling
/// are centrally managed. The API key is passed in the <c>api-key</c> header (Brevo's
/// contract) and is never logged. Failures surface as <see cref="HttpRequestException"/> so
/// callers can decide whether to retry or bubble.
/// </summary>
public sealed class BrevoEmailSender : IEmailSender
{
    private const string ClientName = "brevo";
    private readonly IHttpClientFactory _clientFactory;
    private readonly BrevoOptions _options;
    private readonly ILogger<BrevoEmailSender> _logger;

    public BrevoEmailSender(
        IHttpClientFactory clientFactory,
        IOptions<BrevoOptions> options,
        ILogger<BrevoEmailSender> logger)
    {
        _clientFactory = clientFactory;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentException.ThrowIfNullOrEmpty(message.To);

        using var http = _clientFactory.CreateClient(ClientName);
        http.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Remove("api-key");
        http.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
        http.DefaultRequestHeaders.Add("accept", "application/json");

        var payload = new BrevoInlineRequest
        {
            Sender = new BrevoSender(_options.SenderName, _options.SenderEmail),
            // Brevo doesn't accept a display name we don't have — omit it and let the
            // recipient's mail client render just the address.
            To = new[] { new BrevoRecipient(message.To, string.Empty) },
            Subject = message.Subject,
            HtmlContent = message.HtmlBody,
        };

        using var response = await http.PostAsJsonAsync("smtp/email", payload, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Brevo email send failed for {Email}: {Status} {Body}",
                message.To, (int)response.StatusCode, body);
            throw new HttpRequestException(
                $"Brevo returned {(int)response.StatusCode} while sending the email.");
        }

        _logger.LogInformation("Brevo email delivered to {Email}", message.To);
    }

    private sealed record BrevoSender(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("email")] string Email);

    private sealed record BrevoRecipient(
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("name")] string Name);

    private sealed class BrevoInlineRequest
    {
        [JsonPropertyName("sender")] public BrevoSender Sender { get; set; } = null!;
        [JsonPropertyName("to")] public BrevoRecipient[] To { get; set; } = Array.Empty<BrevoRecipient>();
        [JsonPropertyName("subject")] public string Subject { get; set; } = string.Empty;
        [JsonPropertyName("htmlContent")] public string HtmlContent { get; set; } = string.Empty;
    }
}
