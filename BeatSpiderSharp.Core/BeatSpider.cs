using BeatSpiderSharp.Core.Filters;
using BeatSpiderSharp.Core.Models;
using BeatSpiderSharp.Core.Models.Preset;
using Serilog;

namespace BeatSpiderSharp.Core;

public abstract class BeatSpider
{
    protected SpecialFolders SpecialFolders { get; } = new();

    protected bool Verbose { get; }

    protected BeatSpider(bool verbose)
    {
        Verbose = verbose;
        SetupLogging();
    }

    private void SetupLogging()
    {
        var configuration = new LoggerConfiguration();
        ConfigureLogger(configuration);
        Log.Logger = configuration.CreateLogger();
    }

    protected virtual void ConfigureLogger(LoggerConfiguration configuration)
    {
    }

    protected async Task<IEnumerable<BeatSpiderSong>?> FilterSongsAsync(IEnumerable<BeatSpiderSong> songs, Preset preset)
    {
        var detailFilterOptions = preset.FilterOptions;
        if (detailFilterOptions.Count == 0)
        {
            Log.Warning("No filters specified");
            return songs;
        }

        var detailFilters = detailFilterOptions
            .Select(options => new RootFilter(options))
            .ToList();

        return songs.Where(song => detailFilters.Any(filter => filter.FilterSong(song)));
    }

    protected int OutputSongs(IEnumerable<BeatSpiderSong> songs, Preset preset, string pathTemplate)
    {
        var output = preset.Output;
        if (output.LimitSongs && output.MaxSongs.HasValue && output.MaxSongs.Value > 0)
        {
            Log.Information("Applying count limit: {Count}", output.MaxSongs.Value);
            songs = songs.Take(output.MaxSongs.Value);
        }

        if (Verbose)
        {
            songs = songs.Select(song =>
            {
                Log.Verbose("Song {Bsr} ({Title} - {Mapper}) included", song.Bsr, song.BeatSaverSong.Metadata?.SongName,
                    song.BeatSaverSong.Uploader?.Name);
                return song;
            });
        }

        var consolidated = songs.ToArray();

        // Process name variables
        var playlistFileName = pathTemplate.Replace("[日期]", DateTime.Today.ToString("yyyy-MM-dd")) + ".bplist";
        var playlistPath = Path.Combine(output.PlaylistPath, playlistFileName);
        var songPath = Path.Combine(output.DownloadPath, pathTemplate);  // no replacement on song path

        // TODO
        // if (output.DownloadSongs)
        // {
        //     Log.Information("Querying BeatSaver for map download info");
        //     
        //     Log.Information("Downloading songs to {Path}", songPath);
        //     Parallel.
        // }

        if (output.SavePlaylist)
        {
            Log.Information("Saving playlist to {Path}", playlistPath);
            var exporter = new PlaylistExporter { PostProcess = output.PostProcessPlaylist };
            exporter.Export(consolidated, preset.Name, preset.Author, preset.Description, playlistPath);
        }

        return consolidated.Length;
    }
}
