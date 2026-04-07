using BeatSpiderSharp.Core.Interfaces;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Preset;

namespace BeatSpiderSharp.Core.Filters;

public class RootFilter : ISongFilter
{
    private readonly FilterConfig _config;

    private readonly List<ISongFilter> _filters;

    public RootFilter(FilterConfig config)
    {
        _config = config;
        _filters = [new SongDetailFilter(_config.SongDetailFilter), new LevelDetailFilter(_config.LevelDetailOptions)];
    }

    public bool FilterSong(BeatSpiderSong song)
    {
        return _filters.Count == 0 || _filters.All(filter => filter.FilterSong(song));
    }
}
