using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO.Compression;
using System.Text.RegularExpressions;
using BeatSpiderSharp.Core.Utilities;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Preset;
using BeatSpiderSharp.Shared;
using Microsoft.IO;
using Microsoft.VisualBasic.FileIO;
using Serilog;

namespace BeatSpiderSharp.Core;

public partial class SongDownloader(SongDownloadConfig config) : IDisposable
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

    private readonly HttpClient _httpClient = HttpClientCreator.Create(new SocketsHttpHandler
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

        if (string.IsNullOrWhiteSpace(config.FolderNameTemplate))
        {
            Log.Warning("Folder name template is empty");
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

        var copiableSongs = config is { CopyLocalSongs: true, LocalSongPaths.Count: > 0 }
            ? FileUtils
                .EnumerateDirectories(config.LocalSongPaths)
                .GroupBy(dir => Path.GetFileName(dir), FileUtils.PathComparer)
                .ToImmutableDictionary(grp => grp.Key, grp => grp.First(), FileUtils.PathComparer)
            : null;

        var skippingSongs = config is { SkipExisting: true, ExistingSongPaths.Count: > 0 }
            ? FileUtils
                .EnumerateDirectories(config.ExistingSongPaths)
                .Select(path => Path.GetFileName(path))
                .ToImmutableHashSet(FileUtils.PathComparer)
            : null;

        var failed = new ConcurrentBag<BeatSpiderSong>();

        var pOptions = new ParallelOptions { MaxDegreeOfParallelism = CONCURRENCY, CancellationToken = token };

        await Parallel.ForEachAsync(songs, pOptions, async (song, cToken) =>
        {
            try
            {
                if (!await DownloadSong(song, copiableSongs, skippingSongs, cToken))
                {
                    failed.Add(song);
                }
            }
            catch (OperationCanceledException)
            {
                Log.Warning("Download canceled for song {Song}", song);
                failed.Add(song);
                throw;
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

    private async Task<bool> DownloadSong(BeatSpiderSong song, ImmutableDictionary<string, string>? copiableSongs,
        ImmutableHashSet<string>? skippingSongs, CancellationToken cToken)
    {
        Log.Debug("Downloading song {Song} ({Hash})", song, song.Hash);

        var folderName = GetSongFolderName(song);
        if (string.IsNullOrWhiteSpace(folderName))
        {
            Log.Error("Folder name is empty after template replacement for song {Song} ({Hash})", song, song.Hash);
            return false;
        }

        if (skippingSongs?.Contains(folderName) == true)
        {
            Log.Information("Skipping existing song {Name}", folderName);
            return true;
        }

        var folderPath = Path.Combine(_outDir, folderName);
        if (copiableSongs?.TryGetValue(folderName, out var sourcePath) == true)
        {
            if (FileUtils.PathComparer.Equals(folderPath, sourcePath))
            {
                Log.Warning("Local song is already in the download path, skip copying {FolderPath}", folderPath);
                return true;
            }

            Log.Debug("Copying local song from {Source}", sourcePath);
            try
            {
                if (Directory.Exists(folderPath))
                {
                    Log.Warning("Song folder already exists, merging: {FolderPath}", folderPath);
                    FileSystem.CopyDirectory(sourcePath, folderPath, true);
                }
                else
                {
                    FileSystem.CopyDirectory(sourcePath, folderPath);
                }

                Log.Information("Copied local song: {Song} ({Hash})", song, song.Hash);
                return true;
            }
            catch (Exception e)
            {
                Log.Warning(e, "Failed to copy local song from {Source}, will download instead", sourcePath);
            }
        }

        await using var stream = await GetSongStream(song.BeatSaverSong.LatestVersion.DownloadURL, song.Hash, cToken);
        if (stream is null)
        {
            return false;
        }

        await UnZipSong(stream, folderPath, song.Hash, cToken);
        Log.Information("Downloaded song: {Song} ({Hash})", song, song.Hash);
        return true;
    }

    [GeneratedRegex(@"[^\x00-\x7F]|\+")]
    private partial Regex EnglishRe();

    private string GetSongFolderName(BeatSpiderSong song)
    {
        var name = config.FolderNameTemplate
            .Replace(Templates.BSR, song.Bsr)
            .Replace(Templates.HASH, song.Hash)
            .Replace(Templates.TITLE, song.BeatSaverSong.Name ?? "")
            .Replace(Templates.SONG_NAME, song.BeatSaverSong.Metadata?.SongName ?? "")
            .Replace(Templates.SONG_SUB_NAME, song.BeatSaverSong.Metadata?.SongSubName ?? "")
            .Replace(Templates.SONG_AUTHOR, song.BeatSaverSong.Metadata?.LevelAuthorName ?? "")
            .Replace(Templates.MAPPER, song.BeatSaverSong.Metadata?.LevelAuthorName ?? "");
        name = FileUtils.SanitizeFileName(name);
        if (config.EnglishOnly)
        {
            name = EnglishRe().Replace(name, "_");
        }

        return name;
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

    /// <summary>
    ///     Extracts the zip <paramref name="stream" /> into <paramref name="folderPath" />. The zip is
    ///     extracted to a temporary folder first, then moved to the correct folder.
    ///     If the target folder already exists, the files are merged into it.
    /// </summary>
    private static async Task UnZipSong(Stream stream, string folderPath, string hash, CancellationToken cToken)
    {
        Log.Verbose("Unzipping song to {FolderPath}", folderPath);

        var tempPath = folderPath + hash + ".tmp";
        FileUtils.TryDeleteDirectory(tempPath);

        try
        {
            await using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                await archive.ExtractToDirectoryAsync(tempPath, cToken);
            }

            if (!Directory.Exists(folderPath))
            {
                //This is faster than FileSystem.MoveDirectory
                Directory.Move(tempPath, folderPath);
            }
            else
            {
                Log.Warning("Song folder already exists, merging: {FolderPath}", folderPath);
                FileSystem.MoveDirectory(tempPath, folderPath, true);
            }
        }
        finally
        {
            FileUtils.TryDeleteDirectory(tempPath);
        }
    }
}
