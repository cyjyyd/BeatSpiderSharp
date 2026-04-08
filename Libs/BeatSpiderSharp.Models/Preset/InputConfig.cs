using BeatSpiderSharp.Models.Enums;

namespace BeatSpiderSharp.Models.Preset;

public class InputConfig
{
    public SongInputSource Source { get; set; } = SongInputSource.BeatSaver;

    public IList<string> Playlists { get; init; } = new List<string>();

    public IList<string> ManualInput { get; init; } = new List<string>();
}
