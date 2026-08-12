using System.Reflection;
using NuGet.Versioning;

namespace BeatSpiderSharp.Shared;

public static class Constants
{
    public static readonly SemanticVersion Version;

    static Constants() => Version = GetVersion();

    private static SemanticVersion GetVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();

        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (string.IsNullOrWhiteSpace(version))
        {
            version = assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        }

#if DEBUG
        return SemanticVersion.Parse(version);
#else
        return SemanticVersion.TryParse(version, out var parsed) ? parsed : new SemanticVersion(0, 0, 0);
#endif
    }
}
