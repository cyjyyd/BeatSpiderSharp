using BeatSpiderSharp.Core.Filters;
using BeatSpiderSharp.Core.Utilities;
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
        CancellationToken cToken)
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

        if (output.Playlist.SavePlaylist)
        {
            var template = output.Playlist.FileNameTemplate;
            var playlistFileName = string.IsNullOrWhiteSpace(template)
                ? $"{preset.Name} ({DateTime.Today:yyyy-MM-dd}).bplist"
                : template.Replace(Templates.DATE, DateTime.Today.ToString("yyyy-MM-dd")) + ".bplist";
            playlistFileName = FileUtils.SanitizeFileName(playlistFileName, '_');
            var playlistPath = Path.Combine(output.Playlist.PlaylistDirectory, playlistFileName);
            var exporter = new PlaylistExporter { PostProcess = output.Playlist.PostProcessPlaylist };
            await exporter.ExportAsync(consolidated, preset.Name, preset.Author, preset.Description, playlistPath,
                cToken);
        }

        if (output.SongDownload.DownloadSongs)
        {
            using var songDownloader = new SongDownloader(output.SongDownload);
            var failed = await songDownloader.DownloadSongs(consolidated, cToken);
            if (failed.Count > 0)
            {
                Log.Warning("Failed to download {Count} songs", failed.Count);
                Log.Warning("Failed to download songs: {Failed}", failed);
            }
        }

        return consolidated.Length;
    }
}
