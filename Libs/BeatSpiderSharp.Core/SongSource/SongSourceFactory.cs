using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Blist;
using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib.Types;
using BeatSpiderSharp.Extensions;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Shared;
using Serilog;

namespace BeatSpiderSharp.Core.SongSource;

public class SongSourceFactory : IDisposable
{
    private HttpClient? _httpClient;

    private HttpClient HttpClient => _httpClient ??= HttpClientCreator.Create(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5)
    });

    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }

    public static IAsyncEnumerable<BeatSpiderSong> CreateFromManualSongInput(IList<string> input,
        IAsyncEnumerable<BeatSpiderSong> allSongs)
    {
        var bsrSet = new HashSet<string>();
        var hashSet = new HashSet<string>();

        foreach (var entry in input)
        {
            if (!entry.IsHex())
            {
                Log.Warning("Invalid entry: {Entry}", entry);
            }
            else if (entry.Length == 40)
            {
                hashSet.Add(entry.ToLowerInvariant());
            }
            else
            {
                bsrSet.Add(entry.ToLowerInvariant());
            }
        }

        return allSongs.Where(song =>
            bsrSet.Contains(song.Bsr.ToLowerInvariant()) || hashSet.Contains(song.Hash.ToLowerInvariant()));
    }

    public async Task<IAsyncEnumerable<BeatSpiderSong>> CreateFromPlaylists(IList<string> playlistPaths,
        IAsyncEnumerable<BeatSpiderSong> allSongs, CancellationToken cToken)
    {
        if (playlistPaths.Count == 0)
        {
            Log.Warning("No playlist paths given");
            return AsyncEnumerable.Empty<BeatSpiderSong>();
        }

        var bplistHandler = new LegacyPlaylistHandler();
        var blistHandler = new BlistPlaylistHandler();
        var playlists = new List<IPlaylist>(playlistPaths.Count);
        foreach (var path in playlistPaths)
        {
            Log.Debug("Loading playlist: {PlaylistPath}", path);

            try
            {
                IPlaylist? playlist;
                if (Uri.TryCreate(path, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    Log.Debug("Downloading playlist: {Uri}", uri);
                    var data = await HttpClient.GetByteArrayAsync(uri, cToken);
                    await using var stream = new MemoryStream(data);
                    playlist = bplistHandler.Deserialize(stream);
                }
                else
                {
                    if (!File.Exists(path))
                    {
                        throw new FileNotFoundException("Playlist file not found", path);
                    }

                    var extension = Path.GetExtension(path);

                    if (string.IsNullOrWhiteSpace(extension))
                    {
                        Log.Error("Playlist file has no extension: {PlaylistPath}", path);
                    }

                    playlist = extension switch
                    {
                        ".json" or ".bplist" => bplistHandler.Deserialize(path),
                        ".blist" => blistHandler.Deserialize(path),
                        _ => null
                    };
                }

                if (playlist == null)
                {
                    Log.Error("Playlist format is unknown or is null: {Name}", path);
                }
                else
                {
                    playlists.Add(playlist);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to load playlist: {Name}", path);
                throw;
            }
        }

        var bsrSet = new HashSet<string>();
        var hashSet = new HashSet<string>();
        foreach (var playlistSong in playlists.SelectMany(playlist => playlist))
        {
            if (!string.IsNullOrWhiteSpace(playlistSong.Key))
            {
                bsrSet.Add(playlistSong.Key.ToLowerInvariant());
            }
            else if (!string.IsNullOrWhiteSpace(playlistSong.Hash))
            {
                hashSet.Add(playlistSong.Hash.ToLowerInvariant());
            }
            else
            {
                Log.Warning("Playlist song with neither key nor hash encountered");
            }
        }

        Log.Information("Loaded {SongCount} songs from {PlaylistCount} playlists", bsrSet.Count + hashSet.Count,
            playlists.Count);
        return allSongs.Where(song =>
            bsrSet.Contains(song.Bsr.ToLowerInvariant()) || hashSet.Contains(song.Hash.ToLowerInvariant()));
    }
}
