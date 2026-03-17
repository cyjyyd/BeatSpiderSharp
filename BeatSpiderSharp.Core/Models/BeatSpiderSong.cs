using BeatSpiderSharp.Core.Models.BeatSaver;

namespace BeatSpiderSharp.Core.Models;

public class BeatSpiderSong
{
    public required string Hash { get; init; }

    public required string Bsr { get; init; }

    public required Song BeatSaverSong { get; init; }
    
    // TODO add more info if needed

    public static BeatSpiderSong FromBeatSaverSong(Song song)
    {
        if (!ValidateBeatSaverSong(song))
        {
            throw new ArgumentException("BeatSaver song must have id and hash");
        }
        return new BeatSpiderSong
        {
            Hash = song.LatestVersion.Hash!,
            Bsr = song.Id!,
            BeatSaverSong = song
        };
    }

    public static bool ValidateBeatSaverSong(Song? song) =>
        song != null && !string.IsNullOrWhiteSpace(song.Id)
                     && song.Versions.Count > 0
                     && !string.IsNullOrWhiteSpace(song.LatestVersion.Hash);

    public override string ToString() =>
        $"{Bsr} ({BeatSaverSong.Metadata?.SongName} - {BeatSaverSong.Metadata?.LevelAuthorName})";
}
