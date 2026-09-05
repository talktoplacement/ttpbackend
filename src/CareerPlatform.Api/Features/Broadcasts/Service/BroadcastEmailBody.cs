using System.Net;
using System.Text;
using CareerPlatform.Api.Features.Broadcasts.Domain;

namespace CareerPlatform.Api.Features.Broadcasts.Service;

/// <summary>
/// Renders a <see cref="Broadcast"/> into the HTML body used for <c>Promotion</c> e-mail delivery.
/// Every interpolated value is HTML-encoded: broadcast copy is admin-authored free text and would
/// otherwise be an injection vector into every recipient's mail client.
/// </summary>
internal static class BroadcastEmailBody
{
    public static string Render(Broadcast broadcast)
    {
        ArgumentNullException.ThrowIfNull(broadcast);

        var html = new StringBuilder();
        html.Append("<h2>").Append(Encode(broadcast.Heading)).Append("</h2>");

        foreach (var paragraph in SplitParagraphs(broadcast.Message))
        {
            html.Append("<p>").Append(Encode(paragraph)).Append("</p>");
        }

        if (!string.IsNullOrWhiteSpace(broadcast.QuestionText))
        {
            html.Append("<p><b>").Append(Encode(broadcast.QuestionText)).Append("</b></p>");
        }

        // Only absolute http(s) links are emitted — a relative or `javascript:` href would either
        // break in a mail client or be an XSS vector.
        if (TryAbsoluteHttpUrl(broadcast.QuestionLink, out var href))
        {
            var encoded = Encode(href);
            html.Append("<p><a href=\"").Append(encoded).Append("\">").Append(encoded).Append("</a></p>");
        }

        return html.ToString();
    }

    private static IEnumerable<string> SplitParagraphs(string message) =>
        (message ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static bool TryAbsoluteHttpUrl(string? candidate, out string url)
    {
        url = string.Empty;
        if (string.IsNullOrWhiteSpace(candidate)) return false;
        if (!Uri.TryCreate(candidate.Trim(), UriKind.Absolute, out var parsed)) return false;
        if (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps) return false;
        url = parsed.ToString();
        return true;
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
