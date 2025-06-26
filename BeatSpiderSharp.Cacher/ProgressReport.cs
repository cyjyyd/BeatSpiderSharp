namespace BeatSpiderSharp.Cacher;

public class ProgressReport
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public Exception? Error { get; set; }
}
