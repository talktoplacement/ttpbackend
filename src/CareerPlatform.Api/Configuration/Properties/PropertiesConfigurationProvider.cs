using System.Text;

namespace CareerPlatform.Api.Configuration.Properties;

/// <summary>
/// Configuration provider for Java-style <c>application.properties</c> files.
///
/// Exists so operators can tune runtime values — most importantly subscription pricing — by editing
/// one plain-text file, with no code change and no redeploy. .NET ships JSON/env/INI providers but
/// not a <c>.properties</c> one, so this fills the gap and plugs into the standard configuration
/// pipeline (including <c>reloadOnChange</c>, which lets a price edit take effect on the running
/// process).
///
/// Supported syntax:
/// <code>
///   # comment            (also '!' comment)
///   key = value          (whitespace around '=' and ':' separators is trimmed)
///   key: value
///   a.b.c = value        (dots become the standard ':' configuration hierarchy separator)
///   key = line one \     (a trailing backslash continues the value on the next line)
///          continued
/// </code>
/// Blank lines are ignored. A duplicate key wins on last occurrence, matching Java semantics.
/// Values are stored verbatim otherwise — no type coercion happens here; the options binder does it.
/// </summary>
public sealed class PropertiesConfigurationProvider : FileConfigurationProvider
{
    public PropertiesConfigurationProvider(PropertiesConfigurationSource source) : base(source) { }

    public override void Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Data = Parse(stream);
    }

    /// <summary>
    /// Parses a properties stream into the flat, case-insensitive dictionary the configuration
    /// system expects. Public and static so it is directly unit-testable without a file on disk.
    /// </summary>
    public static IDictionary<string, string?> Parse(Stream stream)
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        var pendingKey = (string?)null;
        var pendingValue = new StringBuilder();

        while (reader.ReadLine() is { } rawLine)
        {
            var line = rawLine.Trim();

            // Continuation of a previous value takes precedence over comment/blank handling so a
            // continued value may legitimately contain '#' or be blank.
            if (pendingKey is not null)
            {
                if (EndsWithContinuation(line))
                {
                    pendingValue.Append(line[..^1].TrimEnd());
                    continue;
                }
                pendingValue.Append(line);
                data[pendingKey] = pendingValue.ToString();
                pendingKey = null;
                pendingValue.Clear();
                continue;
            }

            if (line.Length == 0 || line[0] == '#' || line[0] == '!')
            {
                continue;
            }

            var separatorIndex = IndexOfSeparator(line);
            if (separatorIndex < 0)
            {
                // A bare key with no separator is treated as an empty value, matching Java.
                data[Normalize(line)] = string.Empty;
                continue;
            }

            var key = Normalize(line[..separatorIndex].Trim());
            var value = line[(separatorIndex + 1)..].Trim();

            if (key.Length == 0)
            {
                continue;
            }

            if (EndsWithContinuation(value))
            {
                pendingKey = key;
                pendingValue.Clear();
                pendingValue.Append(value[..^1].TrimEnd());
                continue;
            }

            data[key] = value;
        }

        // A file ending mid-continuation still contributes what it had.
        if (pendingKey is not null)
        {
            data[pendingKey] = pendingValue.ToString();
        }

        return data;
    }

    /// <summary>
    /// A trailing backslash continues the value, but an escaped backslash (<c>\\</c>) does not.
    /// </summary>
    private static bool EndsWithContinuation(string value)
    {
        if (!value.EndsWith('\\')) return false;
        var trailing = 0;
        for (var i = value.Length - 1; i >= 0 && value[i] == '\\'; i--) trailing++;
        return trailing % 2 == 1;
    }

    /// <summary>Finds the first unescaped <c>=</c> or <c>:</c> separator.</summary>
    private static int IndexOfSeparator(string line)
    {
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '\\')
            {
                i++; // skip the escaped character
                continue;
            }
            if (line[i] == '=' || line[i] == ':') return i;
        }
        return -1;
    }

    /// <summary>
    /// Maps a properties key to a .NET configuration key: dots become the ':' section separator, so
    /// <c>pricing.plans.monthly-pro.price</c> binds to <c>Pricing:Plans:monthly-pro:Price</c>.
    /// </summary>
    private static string Normalize(string key) => key.Replace('.', ':');
}
