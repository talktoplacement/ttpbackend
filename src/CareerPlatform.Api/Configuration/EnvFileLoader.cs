using System.Text.RegularExpressions;
using DotNetEnv;

namespace CareerPlatform.Api.Configuration;

/// <summary>
/// Loads an application.properties-style <c>.env</c> file into <see cref="Environment"/> variables
/// so it can override <c>appsettings*.json</c> via the standard ASP.NET Core configuration
/// provider chain. Also translates a Postgres URI in <c>DATABASE_URL</c> into the Npgsql
/// keyword form expected by <c>ConnectionStrings:DefaultConnection</c>, so both formats work.
///
/// Precedence at boot: appsettings.json &lt; appsettings.&lt;Env&gt;.json &lt; .env &lt; real env vars.
/// Values already present in the process environment are NEVER overwritten by the file.
/// </summary>
public static class EnvFileLoader
{
    /// <summary>
    /// Loads <c>.env</c> from the given content root (falling back to CWD), then normalizes
    /// <c>DATABASE_URL</c> into <c>ConnectionStrings__DefaultConnection</c> if the latter is
    /// not already set. Safe to call multiple times.
    /// </summary>
    public static void Load(string? contentRoot = null)
    {
        var candidates = new[]
        {
            Path.Combine(contentRoot ?? Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(Directory.GetCurrentDirectory(), ".env"),
            Path.Combine(AppContext.BaseDirectory, ".env"),
            // Walk up two levels — helps when running from src/CareerPlatform.Api while the
            // .env sits at the backend/ root during local dev.
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"),
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            Env.Load(path, new LoadOptions(setEnvVars: true, clobberExistingVars: false));
            break;
        }

        NormalizeDatabaseUrl();
    }

    /// <summary>
    /// If <c>DATABASE_URL</c> is a <c>postgresql://user:pass@host:port/db</c> URI and
    /// <c>ConnectionStrings__DefaultConnection</c> is not already set, translate it into
    /// the Npgsql keyword form the app expects. Preserves any query-string params.
    /// </summary>
    private static void NormalizeDatabaseUrl()
    {
        const string TargetKey = "ConnectionStrings__DefaultConnection";
        if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(TargetKey)))
        {
            return;
        }

        var url = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(url)) return;

        // URIs with '@' or '#' inside the password break System.Uri. Peel the userinfo off with
        // a regex before handing the rest to Uri.
        // Format: scheme://user:password@host:port/db?query
        var m = Regex.Match(
            url.Trim(),
            @"^(?<scheme>postgres(?:ql)?):\/\/(?<user>[^:@\s]+):(?<pass>.+)@(?<host>[^:/?\s]+)(?::(?<port>\d+))?(?:\/(?<db>[^?\s]+))?(?:\?(?<query>\S*))?$",
            RegexOptions.IgnoreCase);
        if (!m.Success) return;

        var host = m.Groups["host"].Value;
        var port = m.Groups["port"].Success ? m.Groups["port"].Value : "5432";
        var db = m.Groups["db"].Success ? m.Groups["db"].Value : "postgres";
        var user = Uri.UnescapeDataString(m.Groups["user"].Value);
        var pass = Uri.UnescapeDataString(m.Groups["pass"].Value);

        // Certificate validation is ON by default (secure). Operators may opt out per-environment
        // by setting DATABASE_TRUST_SERVER_CERTIFICATE=true — e.g. a managed Postgres whose CA is
        // not in the container trust store during local dev. NEVER trust blindly in production.
        var trustServerCert = string.Equals(
            Environment.GetEnvironmentVariable("DATABASE_TRUST_SERVER_CERTIFICATE"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        var connString =
            $"Host={host};Port={port};Database={db};Username={user};Password={pass};" +
            $"SSL Mode=Require;Trust Server Certificate={trustServerCert.ToString().ToLowerInvariant()};Pooling=true";

        // Confine the app to its own Postgres schema when one is configured.
        //
        // The database may be shared with unrelated applications, and several table names
        // (Transactions, Notifications, Certificates, Sections, SupportTickets,
        // PlacementCompanies) are generic enough to already exist there with a different shape.
        // Without an explicit search path the app would silently bind to a foreign table and fail
        // on the first query. Defaults to `public` so a dedicated database behaves as before.
        var searchPath = Environment.GetEnvironmentVariable("DATABASE_SCHEMA");
        if (!string.IsNullOrWhiteSpace(searchPath))
        {
            connString += $";Search Path={searchPath.Trim()}";
        }

        // Merge query-string params (?sslmode=require&pool_mode=session ...) so operators can
        // override without editing code. Query keys are lowercase-underscore; map the ones we
        // care about into Npgsql keywords.
        if (m.Groups["query"].Success)
        {
            foreach (var kv in m.Groups["query"].Value.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = kv.Split('=', 2);
                if (parts.Length != 2) continue;
                var k = parts[0].ToLowerInvariant();
                var v = Uri.UnescapeDataString(parts[1]);
                switch (k)
                {
                    case "sslmode": connString += $";SSL Mode={v}"; break;
                    case "application_name": connString += $";Application Name={v}"; break;
                }
            }
        }

        Environment.SetEnvironmentVariable(TargetKey, connString);
    }
}
