using Serilog;

namespace BeatSpiderSharp.Core;

public class SpecialFolders: IDisposable
{
    public string DataFolder { get; }
    
    public string TempFolder { get; }

    private bool _disposed;

    public SpecialFolders()
    {
        DataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create), "BeatSpiderSharp");
        TempFolder = Path.Combine(DataFolder, "Temp", Path.GetRandomFileName());

        Directory.CreateDirectory(DataFolder);
        Directory.CreateDirectory(TempFolder);
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        GC.SuppressFinalize(this);
        try
        {
            Directory.Delete(TempFolder, true);
        }
        catch (Exception e)
        {
            Log.Error(e, "Could not delete temporary folder");
        }
    }
}
