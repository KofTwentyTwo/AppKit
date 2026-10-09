using System.Reflection;

namespace KofTwentyTwo.AppKit;

/// <summary>
/// Human-readable build identity: the semantic version plus the short git commit the
/// build came from, e.g. "1.2.0-beta.6 (9ac6c2b1f)" for releases or
/// "1.2.1-dev (3a87f9e02)" for local builds. The release pipeline sets the version
/// from the tag; SourceLink appends the commit as "+&lt;sha&gt;" metadata.
/// </summary>
public static class BuildVersion
{
    private const int ShortHashLength = 9;

    /// <summary>Describes <paramref name="assembly"/>'s version and commit.</summary>
    public static string Describe(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return Format(InformationalVersion(assembly));
    }

    /// <summary>
    /// True when <paramref name="assembly"/> carries a prerelease version (a '-' before
    /// any build metadata), e.g. "1.0.0-beta.1" or a local "1.0.1-dev" build. Installs
    /// of prerelease builds follow the dev update channel.
    /// </summary>
    public static bool IsPrerelease(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        return IsPrerelease(InformationalVersion(assembly));
    }

    /// <summary>True when the version part (before any '+metadata') contains a '-'.</summary>
    public static bool IsPrerelease(string? informationalVersion)
    {
        if (informationalVersion is null)
        {
            return false;
        }

        int plus = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        return (plus >= 0 ? informationalVersion[..plus] : informationalVersion).Contains('-', StringComparison.Ordinal);
    }

    /// <summary>
    /// Formats an informational version: the "+&lt;metadata&gt;" suffix becomes a
    /// parenthesized short commit hash.
    /// </summary>
    public static string Format(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return "unknown";
        }

        int plus = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        if (plus < 0)
        {
            return informationalVersion;
        }

        string version = informationalVersion[..plus];
        string metadata = informationalVersion[(plus + 1)..];
        // Metadata may carry more than the sha (dot-separated); the hash is first.
        int dot = metadata.IndexOf('.', StringComparison.Ordinal);
        string hash = dot >= 0 ? metadata[..dot] : metadata;
        if (hash.Length > ShortHashLength)
        {
            hash = hash[..ShortHashLength];
        }

        return hash.Length == 0 ? version : $"{version} ({hash})";
    }

    private static string? InformationalVersion(Assembly assembly)
        => assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString();
}
