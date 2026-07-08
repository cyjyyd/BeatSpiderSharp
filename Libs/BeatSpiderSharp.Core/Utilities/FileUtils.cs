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
}
