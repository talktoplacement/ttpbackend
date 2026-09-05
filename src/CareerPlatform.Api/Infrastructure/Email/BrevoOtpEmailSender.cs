using System.Net.Http.Json;
using System.Text.Json.Serialization;
using CareerPlatform.Api.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareerPlatform.Api.Infrastructure.Email;

/// <summary>
/// Brevo (v3) transactional-email adapter for OTP delivery. Uses <see cref="HttpClient"/> from
/// <see cref="IHttpClientFactory"/> so timeouts and DNS resolution are managed centrally. The
/// API key is sent in the <c>api-key</c> header (Brevo's contract) and is never logged. Fails
/// loudly — a delivery error becomes a <see cref="HttpRequestException"/> surfaced by the
/// caller to the user, so no user is stranded thinking a code was sent when it was not.
/// </summary>
public sealed class BrevoOtpEmailSender : IOtpEmailSender
{
    private const string ClientName = "brevo";
    private readonly IHttpClientFactory _clientFactory;
    private readonly BrevoOptions _options;
    private readonly ILogger<BrevoOtpEmailSender> _logger;

    public BrevoOtpEmailSender(
        IHttpClientFactory clientFactory,
        IOptions<BrevoOptions> options,
        ILogger<BrevoOtpEmailSender> logger)
    {
        _clientFactory = clientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(
        string recipientEmail,
        string recipientName,
        string code,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrEmpty(recipientEmail);
        ArgumentException.ThrowIfNullOrEmpty(code);

        using var http = _clientFactory.CreateClient(ClientName);
        http.BaseAddress = new Uri(_options.ApiBaseUrl.TrimEnd('/') + "/");
        http.DefaultRequestHeaders.Remove("api-key");
        http.DefaultRequestHeaders.Add("api-key", _options.ApiKey);
        http.DefaultRequestHeaders.Add("accept", "application/json");

        // Prefer the templated payload when the operator has configured a template. The
        // template parameters ({code}, {ttlMinutes}, {name}) are the personalisation slots the
        // operator can drop into their Brevo template body.
        object payload;
        if (_options.OtpTemplateId is int templateId && templateId > 0)
        {
            payload = new BrevoTemplatedRequest
            {
                To = new[] { new BrevoRecipient(recipientEmail, recipientName) },
                TemplateId = templateId,
                Params = new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["name"] = recipientName,
                    ["ttlMinutes"] = ((int)ttl.TotalMinutes).ToString(),
                },
            };
        }
        else
        {
            var minutes = Math.Max(1, (int)ttl.TotalMinutes);
            payload = new BrevoInlineRequest
            {
                Sender = new BrevoSender(_options.SenderName, _options.SenderEmail),
                To = new[] { new BrevoRecipient(recipientEmail, recipientName) },
                Subject = "Your verification code",
                HtmlContent =
                    $"<p>Hi {WebUtility(recipientName)},</p>" +
                    $"<p>Your verification code is <b style=\"font-size:20px\">{WebUtility(code)}</b>.</p>" +
                    $"<p>It expires in {minutes} minute(s).</p>" +
                    "<p>If you didn't request this, you can ignore this email.</p>",
            };
        }

        using var response = await http.PostAsJsonAsync("smtp/email", payload, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Brevo OTP send failed for {Email}: {Status} {Body}",
                recipientEmail, (int)response.StatusCode, body);
            throw new HttpRequestException(
                $"Brevo returned {(int)response.StatusCode} while sending the OTP email.");
        }

        _logger.LogInformation("Brevo OTP delivered to {Email}", recipientEmail);
    }

    /// <summary>Minimal HTML-escape for the inline template. Full HTML sanitisation is out of scope here.</summary>
    private static string WebUtility(string value) =>
        System.Net.WebUtility.HtmlEncode(value ?? string.Empty);

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

    private sealed class BrevoTemplatedRequest
    {
        [JsonPropertyName("to")] public BrevoRecipient[] To { get; set; } = Array.Empty<BrevoRecipient>();
        [JsonPropertyName("templateId")] public int TemplateId { get; set; }
        [JsonPropertyName("params")] public Dictionary<string, string> Params { get; set; } = new();
    }
}
