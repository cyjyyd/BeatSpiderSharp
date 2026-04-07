using BeatSpiderSharp.Models.Preset.FilterOptions;

namespace BeatSpiderSharp.Models.Preset;

public class FilterConfig
{
    public SongDetailOptions SongDetailFilter { get; init; } = new();

    public LevelDetailOptions LevelDetailOptions { get; init; } = new();
}
