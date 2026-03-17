using BeatSpiderSharp.Core.Interfaces;
using BeatSpiderSharp.Core.Models;
using BeatSpiderSharp.Core.Models.Preset;

namespace BeatSpiderSharp.Core.Filters;

public class RootFilter : ISongFilter
{
    private readonly FilterConfig _config;

    private readonly List<ISongFilter> _filters = new(1); // there is currently only one sub filter existing

    public RootFilter(FilterConfig config)
    {
        _config = config;
        _filters.Add(new DetailFilter(_config.DetailFilter));
    }

    public bool FilterSong(BeatSpiderSong song)
    {
        return _filters.Count == 0 || _filters.All(filter => filter.FilterSong(song));
    }
}
