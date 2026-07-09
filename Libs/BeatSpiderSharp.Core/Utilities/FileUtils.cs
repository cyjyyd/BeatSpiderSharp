using System.Buffers;
using Serilog;

namespace BeatSpiderSharp.Core.Utilities;

public static class FileUtils
{
    // Path.GetInvalidFileNameChars() is platform-dependent, need to hard code windows specific invalid chars
    private static readonly SearchValues<char> InvalidFileNameChars = SearchValues.Create(
        [..Path.GetInvalidFileNameChars(), '<', '>', ':', '"', '/', '\\', '|', '?', '*']);

    /// <summary>
    ///     Compares paths using current OS's default filesystem case-sensitivity:
    ///     case-insensitive on Windows and macOS, case-sensitive elsewhere.
    /// </summary>
    /// <remarks>This does not cover non-default cases</remarks>
    public static readonly StringComparer PathComparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

    public static string SanitizeFileName(string fileName, char replacement)
    {
        return string.Concat(fileName.Select(c => InvalidFileNameChars.Contains(c) ? replacement : c)).Trim();
    }

    public static string SanitizeFileName(string fileName)
    {
        return string.Concat(fileName.Where(c => !InvalidFileNameChars.Contains(c))).Trim();
    }
    
    public static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to delete temporary file {Path}", path);
        }
    }

    public static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to delete directory {Path}", path);
        }
    }

    public static IEnumerable<string> EnumerateDirectories(IEnumerable<string> paths, string searchPattern = "*",
        SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        return paths.Where(path =>
            {
                var exists = Directory.Exists(path);
                if (!exists)
                {
                    Log.Warning("Directory {Path} does not exist", path);
                }

                return exists;
            })
            .SelectMany(path =>
            {
                try
                {
                    return Directory.EnumerateDirectories(path, searchPattern, searchOption);
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Failed to enumerate directories in {Path}", path);
                    return [];
                }
            })
            .Select(Path.GetFullPath);
    }
}
