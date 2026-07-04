using System.Collections.Concurrent;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Preset;
using Serilog;

namespace BeatSpiderSharp.Core;

public class SongDownloader(SongDownloadConfig config) : IDisposable
{
    private const int CONCURRENCY = 8;

    private readonly string outDir = Path.GetFullPath(config.DownloadPath);

    private readonly HttpClient _httpClient = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        MaxConnectionsPerServer = CONCURRENCY
    });

    public void Dispose()
    {
        _httpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task<IReadOnlyCollection<BeatSpiderSong>> DownloadSongs(BeatSpiderSong[] songs,
        CancellationToken token)
    {
        Log.Information("Downloading songs to {Path}", outDir);
        if (!Directory.Exists(outDir))
        {
            Log.Error("Output directory does not exist: {OutputDir}", outDir);
            return songs;
        }

        var failed = new ConcurrentBag<BeatSpiderSong>();

        var pOptions = new ParallelOptions { MaxDegreeOfParallelism = CONCURRENCY, CancellationToken = token };

        await Parallel.ForEachAsync(songs, pOptions, async (song, cToken) =>
        {
            try
            {
                if (!await DownloadSong(song, cToken))
                {
                    failed.Add(song);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to download song {Song}", song);
                Log.Debug("Song hash {SongHash}, url {Url}", song.Hash, song.BeatSaverSong.LatestVersion.DownloadURL);
                failed.Add(song);
            }
        });
        return failed;
    }

    private async Task<bool> DownloadSong(BeatSpiderSong song, CancellationToken cToken)
    {
        Log.Verbose("Downloading song {SongHash}", song.Hash);
        var url = song.BeatSaverSong.LatestVersion.DownloadURL;
        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
        {
            Log.Error("Invalid download URL for song {Song}: {Url}", song, url);
            return false;
        }

        var fileName = Path.GetFileName(uri.LocalPath);
        var outputPath = Path.Combine(outDir, fileName);
        if (File.Exists(outputPath))
        {
            Log.Warning("Song already exists: {OutputPath}", outputPath);
            return true; //TODO check config
        }

        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cToken);
        response.EnsureSuccessStatusCode();
        await using var fs = new FileStream(outputPath, FileMode.CreateNew);
        await response.Content.CopyToAsync(fs, cToken);
        Log.Information("Downloaded song: {OutputPath}", outputPath);
        return true;
    }
}
