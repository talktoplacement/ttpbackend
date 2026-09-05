using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace CareerPlatform.Api.Features.Resumes.Service;

/// <summary>
/// Dependency-free resume text extractor. For PDFs it walks the raw <c>stream…endstream</c> blocks,
/// inflates Flate-compressed content with the framework's <see cref="ZLibStream"/>, and pulls the
/// text-showing string literals out of the content operators. It is intentionally best-effort:
/// vector/text PDFs extract well; scanned/image PDFs return little text (the caller treats that as
/// "unreadable" rather than a real low score).
/// </summary>
internal sealed partial class PdfResumeTextExtractor : IResumeTextExtractor
{
    public string ExtractText(byte[] content)
    {
        if (content is null || content.Length == 0) return string.Empty;

        var isPdf = content.Length > 4 &&
            content[0] == '%' && content[1] == 'P' && content[2] == 'D' && content[3] == 'F';
        if (!isPdf)
        {
            // Plain text / markdown / JSON resume content.
            return Normalize(Encoding.UTF8.GetString(content));
        }

        var sb = new StringBuilder();
        foreach (var streamBytes in EnumerateStreams(content))
        {
            var decoded = TryInflate(streamBytes) ?? streamBytes;
            var chunk = Encoding.Latin1.GetString(decoded);
            AppendTextOperators(chunk, sb);
        }
        return Normalize(sb.ToString());
    }

    /// <summary>Yields the raw bytes between each <c>stream</c>/<c>endstream</c> pair.</summary>
    private static IEnumerable<byte[]> EnumerateStreams(byte[] content)
    {
        // Work on a Latin1 view so byte offsets line up 1:1 with char offsets.
        var text = Encoding.Latin1.GetString(content);
        var idx = 0;
        while (true)
        {
            var start = text.IndexOf("stream", idx, StringComparison.Ordinal);
            if (start < 0) yield break;
            var dataStart = start + "stream".Length;
            // Skip the EOL after the 'stream' keyword (CRLF or LF).
            if (dataStart < text.Length && text[dataStart] == '\r') dataStart++;
            if (dataStart < text.Length && text[dataStart] == '\n') dataStart++;

            var end = text.IndexOf("endstream", dataStart, StringComparison.Ordinal);
            if (end < 0) yield break;

            var len = end - dataStart;
            if (len > 0)
            {
                var slice = new byte[len];
                Array.Copy(content, dataStart, slice, 0, len);
                yield return slice;
            }
            idx = end + "endstream".Length;
        }
    }

    /// <summary>Inflates a Flate (zlib) stream; returns null if it is not zlib-compressed.</summary>
    private static byte[]? TryInflate(byte[] data)
    {
        try
        {
            using var input = new MemoryStream(data);
            using var zlib = new ZLibStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            zlib.CopyTo(output);
            return output.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Pulls visible text out of a content stream's Tj/TJ string literals.</summary>
    private static void AppendTextOperators(string content, StringBuilder sb)
    {
        foreach (Match m in PdfLiteralRegex().Matches(content))
        {
            var raw = m.Groups[1].Value;
            // Unescape the common PDF literal escapes.
            var text = raw
                .Replace(@"\(", "(").Replace(@"\)", ")").Replace(@"\\", "\\")
                .Replace(@"\n", " ").Replace(@"\r", " ").Replace(@"\t", " ");
            if (text.Any(char.IsLetterOrDigit))
            {
                sb.Append(text);
                sb.Append(' ');
            }
        }
    }

    private static string Normalize(string s)
    {
        // Collapse runs of whitespace and control chars into single spaces.
        var cleaned = ControlRegex().Replace(s, " ");
        return WhitespaceRegex().Replace(cleaned, " ").Trim();
    }

    // Parenthesised string literals, allowing escaped parens/backslashes inside.
    [GeneratedRegex(@"\(((?:\\.|[^()\\])*)\)")]
    private static partial Regex PdfLiteralRegex();

    [GeneratedRegex(@"[\x00-\x08\x0B\x0C\x0E-\x1F]")]
    private static partial Regex ControlRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
