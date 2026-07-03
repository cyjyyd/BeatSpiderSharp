using BeatSpiderSharp.Core.Filters;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Enums;
using BeatSpiderSharp.Models.Preset;
using Serilog;

namespace BeatSpiderSharp.Core;

public abstract class BeatSpider : IDisposable
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

    void IDisposable.Dispose()
    {
        SpecialFolders.Dispose();
        GC.SuppressFinalize(this);
    }

    protected IAsyncEnumerable<BeatSpiderSong> FilterSongs(IAsyncEnumerable<BeatSpiderSong> songs, Preset preset)
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

    protected async Task<int> OutputSongsAsync(IAsyncEnumerable<BeatSpiderSong> songs, Preset preset,
        string pathTemplate, CancellationToken cToken)
    {
        var output = preset.Output;
        if (output.SortType == SortType.Rating)
        {
            Log.Information("Sorting songs by rating");
            songs = songs.OrderByDescending(song => song.BeatSaverSong.Stats?.Score);
        }
        
        if (output.LimitSongs && output.MaxSongs.HasValue && output.MaxSongs.Value > 0)
        {
            Log.Information("Applying count limit: {Count}", output.MaxSongs.Value);
            songs = songs.Take(output.MaxSongs.Value);
        }

        if (Verbose)
        {
            if (output.SortType == SortType.Rating)
            {
                songs = songs.Select(song =>
                {
                    Log.Verbose("Song {Bsr} ({Title} - {Mapper}) included at {Rating:P} rating",
                        song.Bsr,
                        song.BeatSaverSong.Metadata?.SongName,
                        song.BeatSaverSong.Uploader?.Name, song.BeatSaverSong.Stats?.Score);
                    return song;
                });
            }
            else
            {
                songs = songs.Select(song =>
                {
                    Log.Verbose("Song {Bsr} ({Title} - {Mapper}) included", song.Bsr,
                        song.BeatSaverSong.Metadata?.SongName,
                        song.BeatSaverSong.Uploader?.Name);
                    return song;
                });
            }
        }

        var consolidated = await songs.ToArrayAsync(cToken);

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
            await exporter.ExportAsync(consolidated, preset.Name, preset.Author, preset.Description, playlistPath,
                cToken);
        }

        return consolidated.Length;
    }
}
