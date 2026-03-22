using BeatSpiderSharp.Models.Enums;

namespace BeatSpiderSharp.Models.Preset;

public class InputConfig
{
    public SongInputSource Source { get; set; } = SongInputSource.BeatSaver;

    public IList<string> Playlists { get; set; } = new List<string>();

    public IList<string> ManualInput { get; set; } = new List<string>();
}
