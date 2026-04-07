using System.Runtime.CompilerServices;
using BeatSpiderSharp.Core.Interfaces;
using BeatSpiderSharp.Core.Utilities;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Enums;
using BeatSpiderSharp.Models.Preset.FilterOptions;
using Serilog;

namespace BeatSpiderSharp.Core.Filters;

public class SongDetailFilter : ISongFilter
{
    private readonly SongDetailOptions _options;

    public SongDetailFilter(SongDetailOptions options)
    {
        _options = options;
        if (options.Downloads.Enable || options.Plays.Enable)
        {
            throw new Exception("Downloads and Plays filters are not supported as BeatSaver do not keep track of them");
        }
    }

    public bool FilterSong(BeatSpiderSong song)
    {
        var filter = _options;
        var map = song.BeatSaverSong;
        var latest = map.LatestVersion;
        var diffs = latest.Diffs;
        var stats = map.Stats;

        if (filter.UploaderId && (map.Uploader == null || !filter.UploaderId.SatisfiedBy(map.Uploader.Id)))
        {
            LogExclusion(song, "Required uploader id not found");
            return false;
        }

        if (filter.UploaderName && (string.IsNullOrEmpty(map.Uploader?.Name) ||
                                    !filter.UploaderName.SatisfiedBy(map.Uploader.Name)))
        {
            LogExclusion(song, "Required uploader name not found");
            return false;
        }

        if (filter.UploadTime && !filter.UploadTime.InRange(map.Uploaded))
        {
            LogExclusion(song, "Upload time not in range");
            return false;
        }

        if (filter.IncludeTags && !filter.IncludeTags.SatisfiedBy(map.Tags))
        {
            LogExclusion(song, "Required tags not found");
            return false;
        }

        if (filter.ExcludeTags && !filter.ExcludeTags.SatisfiedBy(map.Tags))
        {
            LogExclusion(song, "Excluded tags found");
            return false;
        }

        if (filter.UpVotes && !filter.UpVotes.InRange(stats?.Upvotes))
        {
            LogExclusion(song, "Up votes not in range");
            return false;
        }

        if (filter.UpVotePercentage && stats?.Upvotes + stats?.Downvotes > 0 &&
            !filter.UpVotePercentage.InRange(stats?.Upvotes * 100f / (stats?.Upvotes + stats?.Downvotes)))
        {
            LogExclusion(song, "Up vote percentage not in range");
            return false;
        }

        if (filter.DownVotes && !filter.DownVotes.InRange(stats?.Downvotes))
        {
            LogExclusion(song, "Down votes not in range");
            return false;
        }

        if (filter.DownVotePercentage && stats?.Upvotes + stats?.Downvotes > 0 &&
            !filter.DownVotePercentage.InRange(stats?.Downvotes * 100f / (stats?.Upvotes + stats?.Downvotes)))
        {
            LogExclusion(song, "Down vote percentage not in range");
            return false;
        }

        if (filter.Rating && !filter.Rating.InRange(map.Stats?.Score * 100))
        {
            LogExclusion(song, "Rating not in range");
            return false;
        }

        if (filter.FullSpread)
        {
            var ifFullSpread = diffs
                .GroupBy(diff => diff.GetMCharacteristic())
                .Where(group => group.Key is not (null or MCharacteristic.Lightshow))
                .Any(group =>
                    group.DistinctBy(diff => diff.Difficulty).Count() == Enum.GetValues<MDifficulty>().Length
                );
            if (ifFullSpread != filter.FullSpread.Filter)
            {
                LogExclusion(song, "Not full spread");
                return false;
            }
        }

        if (filter.AutoMapper)
        {
            if (filter.AutoMapper.Filter != map.Automapper)
            {
                LogExclusion(song, "Automapper status not matching");
                return false;
            }
        }

        if (filter.Bpm && !filter.Bpm.InRange(map.Metadata?.Bpm))
        {
            LogExclusion(song, "BPM not in range");
            return false;
        }

        if (filter.Duration && !filter.Duration.InRange(map.Metadata?.Duration))
        {
            LogExclusion(song, "Duration not in range");
            return false;
        }

        if (filter.ScoreSaberRanking && !filter.ScoreSaberRanking.SatisfiedBy(map.RankingStatus))
        {
            LogExclusion(song, "Required ScoreSaber ranking status not found");
            return false;
        }

        if (filter.BeatLeaderRanking && !filter.BeatLeaderRanking.SatisfiedBy(map.BlRankingStatus))
        {
            LogExclusion(song, "Required BeatLeader ranking status not found");
            return false;
        }

        if (filter.SageScore && !filter.SageScore.InRange(latest.SageScore))
        {
            LogExclusion(song, "SageScore not in range");
            return false;
        }

        // TODO Implement chinese filter
        // if (filter.FilterChinese)
        // {
        //     // ??
        // }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogExclusion(BeatSpiderSong song, string reason)
    {
        Log.Verbose("Song {Bsr} excluded: {Reason}", song.Bsr, reason);
    }
}
