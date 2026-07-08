using System.Collections.Concurrent;
using BeatSpiderSharp.Core.Utilities;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Preset;
using Microsoft.IO;
using Serilog;

namespace BeatSpiderSharp.Core;

public class SongDownloader(SongDownloadConfig config) : IDisposable
{
    private const int CONCURRENCY = 8;

    private static readonly RecyclableMemoryStreamManager StreamManager = new(new RecyclableMemoryStreamManager.Options
    {
        UseExponentialLargeBuffer = true,
        LargeBufferMultiple = 1024 * 1024,
        MaximumBufferSize = 128 * 1024 * 1024,
        MaximumLargePoolFreeBytes = 128L * 1024 * 1024,
        MaximumSmallPoolFreeBytes = 16L * 1024 * 1024,
        MaximumStreamCapacity = 256L * 1024 * 1024 // Hard limit to guard against bogus lengths
    });

    private string _outDir = string.Empty;
    private string _zipDir = string.Empty;
    private bool _zipDirValid;

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
        if (string.IsNullOrEmpty(config.DownloadPath))
        {
            Log.Error("Output directory path is empty");
            return songs;
        }

        _outDir = Path.GetFullPath(config.DownloadPath);
        if (!Directory.Exists(_outDir))
        {
            Log.Error("Output directory does not exist: {OutputDir}", _outDir);
            return songs;
        }

        Log.Information("Downloading songs to {Path}", _outDir);


        if (config.UseLocalZips || config.SaveZips)
        {
            if (string.IsNullOrEmpty(config.LocalZipsPath))
            {
                Log.Warning("Local zip directory path is empty");
            }
            else
            {
                _zipDir = Path.GetFullPath(config.LocalZipsPath);
                if (!Directory.Exists(_zipDir))
                {
                    Log.Warning("Local zip directory does not exist: {ZipDir}", _zipDir);
                }
                else
                {
                    Log.Information("Using local zip directory: {ZipDir}", _zipDir);
                    _zipDirValid = true;
                }
            }
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
        Log.Debug("Downloading song {Song} ({Hash})", song, song.Hash);

        //TODO check existing song and skip if already downloaded
        //TODO check existing song and copy if already downloaded

        await using var stream = await GetSongStream(song.BeatSaverSong.LatestVersion.DownloadURL, song.Hash, cToken);
        if (stream is null)
        {
            return false;
        }

        try
        {
            // TODO Unzip 
            // await UnZipSong(stream, ...);
            Log.Information("Downloaded song: {Song} ({Hash})", song, song.Hash);
            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Failed to download song {Song}", song);
            return false;
        }
    }

    private async Task<Stream?> GetSongStream(string? url, string hash, CancellationToken cToken)
    {
        var zipName = $"{hash}.zip";
        if (config.UseLocalZips && _zipDirValid)
        {
            var zipPath = Path.Combine(_zipDir, zipName);
            if (File.Exists(zipPath))
            {
                Log.Verbose("Using local song zip: {ZipPath}", zipPath);
                return File.OpenRead(zipPath);
            }
        }

        if (!Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
        {
            Log.Error("Invalid song download URL: {Url}", url);
            return null;
        }

        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cToken);
        response.EnsureSuccessStatusCode();

        var stream = config.SaveZips && _zipDirValid
            ? await SaveZipToDisk(response.Content, Path.Combine(_zipDir, zipName), cToken)
            : await BufferZipToMemory(response.Content, hash, cToken);

        stream.Seek(0, SeekOrigin.Begin);
        return stream;
    }

    /// <summary>
    ///     Save the response to a temporary file and promotes it on success
    /// </summary>
    /// <returns>A read-only stream of the saved file</returns>
    private static async Task<Stream> SaveZipToDisk(HttpContent content, string zipPath, CancellationToken cToken)
    {
        var tempPath = zipPath + ".part";
        try
        {
            await using (var writeStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
            {
                await content.CopyToAsync(writeStream, cToken);
            }

            File.Move(tempPath, zipPath, true);
        }
        catch
        {
            FileUtils.TryDeleteFile(tempPath);
            throw;
        }

        Log.Debug("Saved song zip to {ZipPath}", zipPath);
        return File.OpenRead(zipPath);
    }

    /// <summary>
    ///     Buffers the response into a pooled memory stream.
    /// </summary>
    private static async Task<Stream> BufferZipToMemory(HttpContent content, string hash, CancellationToken cToken)
    {
        var capacity = content.Headers.ContentLength ?? 0;
        var memoryStream = StreamManager.GetStream(hash, capacity, true);
        try
        {
            await content.CopyToAsync(memoryStream, cToken);
        }
        catch
        {
            // Return the buffer back to the pool
            await memoryStream.DisposeAsync();
            throw;
        }

        return memoryStream;
    }

    private async Task<bool> UnZipSong(Stream stream, string path)
    {
        Log.Verbose("Unzipping song to {Song}", path);
        // todo
        await Task.Delay(350);
        return true;
    }
}
