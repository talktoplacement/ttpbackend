using Microsoft.Extensions.FileProviders;

namespace CareerPlatform.Api.Configuration.Properties;

/// <summary>
/// <see cref="FileConfigurationSource"/> for Java-style <c>.properties</c> files, wired to
/// <see cref="PropertiesConfigurationProvider"/>.
/// </summary>
public sealed class PropertiesConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        EnsureDefaults(builder);
        return new PropertiesConfigurationProvider(this);
    }
}

/// <summary>
/// Registration helpers for <c>.properties</c> configuration files.
/// </summary>
public static class PropertiesConfigurationExtensions
{
    /// <summary>
    /// Adds a Java-style <c>.properties</c> file to the configuration pipeline.
    /// </summary>
    /// <param name="builder">The configuration builder.</param>
    /// <param name="path">Path to the file, relative to the content root.</param>
    /// <param name="optional">
    /// When <c>true</c> a missing file is ignored, so a deployment that supplies these values as
    /// real environment variables instead does not need the file present.
    /// </param>
    /// <param name="reloadOnChange">
    /// When <c>true</c> the file is watched and the configuration is rebuilt on save. This is what
    /// allows an operator to change a price on a running instance without a restart.
    /// </param>
    public static IConfigurationBuilder AddPropertiesFile(
        this IConfigurationBuilder builder,
        string path,
        bool optional = true,
        bool reloadOnChange = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return builder.Add(new PropertiesConfigurationSource
        {
            Path = path,
            Optional = optional,
            ReloadOnChange = reloadOnChange,
        });
    }

    /// <summary>
    /// Adds an operator settings file by searching the usual locations for it, and returns the
    /// absolute path that was used (or <c>null</c> when the file was not found anywhere).
    ///
    /// Probing is necessary because the process's content root differs by how it is launched:
    /// <c>dotnet run</c> from the project folder, <c>dotnet Api.dll</c> from a publish output, and a
    /// container image all resolve a bare relative path differently. Mirrors the strategy
    /// <see cref="EnvFileLoader"/> already uses for <c>.env</c> so both files behave the same way.
    /// </summary>
    public static string? AddOperatorPropertiesFile(
        this IConfigurationBuilder builder,
        string fileName,
        bool reloadOnChange = true)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        foreach (var candidate in ProbePaths(fileName))
        {
            if (!File.Exists(candidate)) continue;

            var full = Path.GetFullPath(candidate);
            // An absolute path needs its own file provider rooted at the containing directory,
            // otherwise the change-watcher resolves against the content root and never fires.
            builder.Add(new PropertiesConfigurationSource
            {
                Path = Path.GetFileName(full),
                Optional = false,
                ReloadOnChange = reloadOnChange,
                FileProvider = new PhysicalFileProvider(Path.GetDirectoryName(full)!),
            });
            return full;
        }

        return null;
    }

    private static IEnumerable<string> ProbePaths(string fileName)
    {
        yield return Path.Combine(Directory.GetCurrentDirectory(), fileName);
        yield return Path.Combine(AppContext.BaseDirectory, fileName);

        // Walk up from the current directory so running inside src/<Project> still finds a file kept
        // at the repository/backend root next to .env.
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        for (var i = 0; i < 5 && dir is not null; i++)
        {
            yield return Path.Combine(dir.FullName, fileName);
            dir = dir.Parent;
        }

        // Same walk from the binary location, which is what a published/container layout uses.
        var baseDir = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 6 && baseDir is not null; i++)
        {
            yield return Path.Combine(baseDir.FullName, fileName);
            baseDir = baseDir.Parent;
        }
    }
}
