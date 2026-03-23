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
    }

    public bool FilterSong(BeatSpiderSong song)
    {
        var filter = _options;
        var map = song.BeatSaverSong;
        var latest = map.LatestVersion;
        var diffs = latest.Diffs;
        var stats = map.Stats;

        if (filter.UploaderId && filter.UploaderId.Filter != null &&
            map.Uploader?.Id != filter.UploaderId.Filter.Value)
        {
            LogExclusion(song, "Uploader id doesn't match");
            return false;
        }

        if (filter.UploaderName && !string.IsNullOrWhiteSpace(filter.UploaderName.Filter) &&
            !filter.UploaderName.Filter.Equals(map.Uploader?.Name, StringComparison.InvariantCultureIgnoreCase))
        {
            LogExclusion(song, "Uploader name doesn't match");
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
            var pass = diffs
                .GroupBy(diff => diff.GetMCharacteristic())
                .Where(group => group.Key is not (null or MCharacteristic.Lightshow))
                .Any(group =>
                    group.DistinctBy(diff => diff.Difficulty).Count() == Enum.GetValues<MDifficulty>().Length
                );
            if (!pass)
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

        if (filter.Njs && !diffs.Any(diff => filter.Njs.InRange(diff.Njs)))
        {
            LogExclusion(song, "NJS not in range");
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

        if (filter.Walls && !diffs.Any(diff => filter.Walls.InRange(diff.Obstacles)))
        {
            LogExclusion(song, "Walls not in range");
            return false;
        }

        if (filter.ScoreSaberRanking)
        {
            var pass = filter.ScoreSaberRanking.Filter.Any(status => status switch 
            {
                RankingStatus.Unranked => map is { Ranked: false, Qualified: false },
                RankingStatus.Ranked => map.Ranked,
                RankingStatus.Qualified => map.Qualified,
                _ => false
            });

            if (!pass)
            {
                LogExclusion(song, "Required ScoreSaber ranking status not found");
                return false;
            }
        }

        if (filter.BeatLeaderRanking)
        {
            var pass = filter.BeatLeaderRanking.Filter.Any(status => status switch 
            {
                RankingStatus.Unranked => map is { BlRanked: false, BlQualified: false },
                RankingStatus.Ranked => map.BlRanked,
                RankingStatus.Qualified => map.BlQualified,
                _ => false
            });

            if (!pass)
            {
                LogExclusion(song, "Required BeatLeader ranking status not found");
                return false;
            }
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
