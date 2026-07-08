using Serilog;

namespace BeatSpiderSharp.Core.Utilities;

public static class FileUtils
{
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
