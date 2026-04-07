using BeatSpiderSharp.Core.Interfaces;
using BeatSpiderSharp.Core.Utilities;
using BeatSpiderSharp.Extensions;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Enums;
using BeatSpiderSharp.Models.Preset.FilterOptions;
using Serilog;

namespace BeatSpiderSharp.Core.Filters;

public class DetailFilter: ISongFilter
{
    private readonly DetailOptions _options;
    
    public DetailFilter(DetailOptions options)
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
        
        if (filter.IncludeCharacteristics)
        {
            var mapCharas = diffs.Select(diff => diff.GetMCharacteristic()).SelectNotNull().ToHashSet();
            if (!filter.IncludeCharacteristics.SatisfiedBy(mapCharas))
            {
                LogExclusion(song, "Required characteristics not found");
                return false;
            }
        }
        
        if (filter.IncludeDifficulties)
        {
            var mapDiffs = diffs.Select(diff => diff.GetMDifficulty()).SelectNotNull().ToHashSet();
            if (!filter.IncludeDifficulties.SatisfiedBy(mapDiffs))
            {
                LogExclusion(song, "Required difficulties not found");
                return false;
            }
        }

        if (filter.RequireMods)
        {
            var pass = diffs.Any(diff => filter.RequireMods.SatisfiedBy(diff.GetMMods()));
            if (!pass)
            {
                LogExclusion(song, "Required mods not found");
                return false;
            }
        }
        
        if (filter.ExcludeMods)
        {
            if (diffs.All(diff => !filter.ExcludeMods.SatisfiedBy(diff.GetMMods())))
            {
                LogExclusion(song, "All difficulties contain excluded mods");
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

        if (filter.Seconds && !diffs.Any(diff => filter.Seconds.InRange(diff.Seconds)))
        {
            LogExclusion(song, "Seconds not in range");
            return false;
        }

        if (filter.Beats && !diffs.Any(diff => filter.Beats.InRange(diff.Length)))
        {
            LogExclusion(song, "Beats not in range");
            return false;
        }

        if (filter.Njs && !diffs.Any(diff => filter.Njs.InRange(diff.Njs)))
        {
            LogExclusion(song, "NJS not in range");
            return false;
        }

        if (filter.Offset && !diffs.Any(diff => filter.Offset.InRange(diff.Offset)))
        {
            LogExclusion(song, "Offset not in range");
            return false;
        }

        if (filter.Nps && !diffs.Any(diff => filter.Nps.InRange(diff.Nps)))
        {
            LogExclusion(song, "NPS not in range");
            return false;
        }

        if (filter.Notes && !diffs.Any(diff => filter.Notes.InRange(diff.Notes)))
        {
            LogExclusion(song, "Notes not in range");
            return false;
        }

        if (filter.Bombs && !diffs.Any(diff => filter.Bombs.InRange(diff.Bombs)))
        {
            LogExclusion(song, "Bombs not in range");
            return false;
        }

        if (filter.Events && !diffs.Any(diff => filter.Events.InRange(diff.Events)))
        {
            LogExclusion(song, "Events not in range");
            return false;
        }

        if (filter.Walls && !diffs.Any(diff => filter.Walls.InRange(diff.Obstacles)))
        {
            LogExclusion(song, "Walls not in range");
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

        if (filter.ScoreSaberStars)
        {
            var pass = diffs.Any(diff => filter.ScoreSaberStars.InRange(diff.Stars));
            if (!pass)
            {
                LogExclusion(song, "ScoreSaber stars not in range");
                return false;
            }
        }
        
        if (filter.BeatLeaderStars)
        {
            var pass = diffs.Any(diff => filter.BeatLeaderStars.InRange(diff.BlStars));
            if (!pass)
            {
                LogExclusion(song, "BeatLeader stars not in range");
                return false;
            }
        }

        if (filter.ParityErrors && !diffs.Any(diff => filter.ParityErrors.InRange(diff.ParitySummary?.Errors)))
        {
            LogExclusion(song, "ParityErrors not in range");
            return false;
        }

        if (filter.ParityWarns && !diffs.Any(diff => filter.ParityWarns.InRange(diff.ParitySummary?.Warns)))
        {
            LogExclusion(song, "ParityWarns not in range");
            return false;
        }

        if (filter.ParityResets && !diffs.Any(diff => filter.ParityResets.InRange(diff.ParitySummary?.Resets)))
        {
            LogExclusion(song, "ParityResets not in range");
            return false;
        }

        if (filter.SageScore && !filter.SageScore.InRange(latest.SageScore))
        {
            LogExclusion(song, "SageScore not in range");
            return false;
        }

        if (filter.MaxScore && !diffs.Any(diff => filter.MaxScore.InRange(diff.MaxScore)))
        {
            LogExclusion(song, "MaxScore not in range");
            return false;
        }

        // TODO Implement chinese filter
        // if (filter.FilterChinese)
        // {
        //     // ??
        // }
        return true;
    }
    
    private void LogExclusion(BeatSpiderSong song, string reason)
    {
        Log.Verbose("Song {Bsr} excluded: {Reason}", song.Bsr, reason);
    }
}
