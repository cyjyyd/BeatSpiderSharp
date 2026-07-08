using System.Buffers;
using Serilog;

namespace BeatSpiderSharp.Core.Utilities;

public static class FileUtils
{
    // Path.GetInvalidFileNameChars() is platform-dependent, need to hard coded windows specific invalid chars
    private static readonly SearchValues<char> InvalidFileNameChars = SearchValues.Create(
        [..Path.GetInvalidFileNameChars(), '<', '>', ':', '"', '/', '\\', '|', '?', '*']);

    public static string SanitizeFileName(string fileName, char replacement)
    {
        return string.Concat(fileName.Select(c => InvalidFileNameChars.Contains(c) ? replacement : c));
    }

    public static string SanitizeFileName(string fileName)
    {
        return string.Concat(fileName.Where(c => !InvalidFileNameChars.Contains(c)));
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
}
