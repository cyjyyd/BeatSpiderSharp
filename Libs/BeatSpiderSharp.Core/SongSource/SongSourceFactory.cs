using BeatSaberPlaylistsLib;
using BeatSaberPlaylistsLib.Blist;
using BeatSaberPlaylistsLib.Legacy;
using BeatSaberPlaylistsLib.Types;
using BeatSpiderSharp.Extensions;
using BeatSpiderSharp.Models;
using Serilog;

namespace BeatSpiderSharp.Core.SongSource;

public class SongSourceFactory
{
    public static IEnumerable<BeatSpiderSong> CreateFromManualSongInput(IList<string> input,
        IEnumerable<BeatSpiderSong> allSongs)
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

    public static IEnumerable<BeatSpiderSong> CreateFromPlaylists(IList<string> playlistPaths,
        IEnumerable<BeatSpiderSong> allSongs)
    {
        if (playlistPaths.Count == 0)
        {
            Log.Warning("No playlist paths given");
            return [];
        }

        var bplistHandler = new LegacyPlaylistHandler();
        var blistHandler = new BlistPlaylistHandler();
        var playlists = new List<IPlaylist>(playlistPaths.Count);
        foreach (var path in playlistPaths)
        {
            Log.Debug("Loading playlist: {PlaylistPath}", path);
            if (!File.Exists(path))
            {
                Log.Warning("Playlist file not found: {PlaylistPath}", path);
                continue;
            }

            var extension = Path.GetExtension(path);

            if (string.IsNullOrWhiteSpace(extension))
            {
                Log.Error("Playlist file has no extension: {PlaylistPath}", path);
            }

            try
            {
                var playlist = extension switch
                {
                    ".json" or ".bplist" => bplistHandler.Deserialize(path),
                    ".blist" => blistHandler.Deserialize(path),
                    _ => null
                };

                if (playlist == null)
                {
                    Log.Error("Playlist format is unknown or is null: {Name}", path);
                }
                else
                {
                    playlists.Add(playlist);
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to load playlist: {Name}", path);
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
