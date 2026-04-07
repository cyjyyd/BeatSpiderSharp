using System.Diagnostics;
using System.Runtime.CompilerServices;
using BeatSpiderSharp.Core.Interfaces;
using BeatSpiderSharp.Core.Utilities;
using BeatSpiderSharp.Extensions;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.BeatSaver;
using BeatSpiderSharp.Models.Preset.FilterOptions;
using Serilog;

namespace BeatSpiderSharp.Core.Filters;

public class LevelDetailFilter(LevelDetailOptions options) : ISongFilter
{
    public bool FilterSong(BeatSpiderSong song)
    {
        var filter = options;
        var map = song.BeatSaverSong;
        var diffs = map.LatestVersion.Diffs;

        // filter levels by Characteristics and Difficulty filter first
        if (filter.IncludeCharacteristics)
        {
            var mapCharas = diffs.Select(diff => diff.GetMCharacteristic()).SelectNotNull().ToHashSet();
            if (!filter.IncludeCharacteristics.SatisfiedBy(mapCharas))
            {
                LogExclusion(song.Bsr, "Required characteristics not found");
                return false;
            }

            if (filter.IncludeCharacteristics.Filter.Count > 0)
            {
                diffs = diffs.Where(diff =>
                {
                    var mapChara = diff.GetMCharacteristic();
                    return mapChara.HasValue && filter.IncludeCharacteristics.Filter.Contains(mapChara.Value);
                }).ToList();
            }
        }

        if (filter.IncludeDifficulties)
        {
            var mapDiffs = diffs.Select(diff => diff.GetMDifficulty()).SelectNotNull().ToHashSet();
            if (!filter.IncludeDifficulties.SatisfiedBy(mapDiffs))
            {
                LogExclusion(song.Bsr,
                    "Required difficulties not found in the levels satisfying the characteristics filter");
                return false;
            }

            if (filter.IncludeDifficulties.Filter.Count > 0)
            {
                diffs = diffs.Where(diff =>
                {
                    var mapDiff = diff.GetMDifficulty();
                    return mapDiff.HasValue && filter.IncludeDifficulties.Filter.Contains(mapDiff.Value);
                }).ToList();
            }
        }

        Debug.Assert(diffs.Count > 0, "No difficulties left after chara/diff filter");
#if DEBUG
        Log.Verbose("Levels remaining after chara/diff filter: {Levels}", diffs.Count);
#endif

        var result = diffs.Where(diff => FilterLevel(song.Bsr, diff));
        //TODO support Playlist diff highlighting
        if (!result.Any())
        {
            LogExclusion(song.Bsr, "No difficulties satisfying the level detail filters");
            return false;
        }

        return true;
    }

    private bool FilterLevel(string bsr, Diff diff)
    {
        var filter = options;
        // Filter the remaining levels
        if (filter.RequireMods && !filter.RequireMods.SatisfiedBy(diff.GetMMods()))
        {
            LogExclusion(bsr, diff, "Required mods not found");
            return false;
        }

        if (filter.ExcludeMods && !filter.ExcludeMods.SatisfiedBy(diff.GetMMods()))
        {
            LogExclusion(bsr, diff, "Excluded mods found");
            return false;
        }

        if (filter.Seconds && !filter.Seconds.InRange(diff.Seconds))
        {
            LogExclusion(bsr, diff, "Seconds not in range");
            return false;
        }

        if (filter.Beats && !filter.Beats.InRange(diff.Length))
        {
            LogExclusion(bsr, diff, "Beats not in range");
            return false;
        }

        if (filter.Njs && !filter.Njs.InRange(diff.Njs))
        {
            LogExclusion(bsr, diff, "NJS not in range");
            return false;
        }

        if (filter.Offset && !filter.Offset.InRange(diff.Offset))
        {
            LogExclusion(bsr, diff, "Offset not in range");
            return false;
        }

        if (filter.Nps && !filter.Nps.InRange(diff.Nps))
        {
            LogExclusion(bsr, diff, "NPS not in range");
            return false;
        }

        if (filter.Notes && !filter.Notes.InRange(diff.Notes))
        {
            LogExclusion(bsr, diff, "Notes not in range");
            return false;
        }

        if (filter.Bombs && !filter.Bombs.InRange(diff.Bombs))
        {
            LogExclusion(bsr, diff, "Bombs not in range");
            return false;
        }

        if (filter.Events && !filter.Events.InRange(diff.Events))
        {
            LogExclusion(bsr, diff, "Events not in range");
            return false;
        }

        if (filter.Walls && !filter.Walls.InRange(diff.Obstacles))
        {
            LogExclusion(bsr, diff, "Walls not in range");
            return false;
        }

        if (filter.ScoreSaberStars && !filter.ScoreSaberStars.InRange(diff.Stars))
        {
            LogExclusion(bsr, diff, "ScoreSaber stars not in range");
            return false;
        }

        if (filter.BeatLeaderStars && !filter.BeatLeaderStars.InRange(diff.BlStars))
        {
            LogExclusion(bsr, diff, "BeatLeader stars not in range");
            return false;
        }

        if (filter.ParityErrors && !filter.ParityErrors.InRange(diff.ParitySummary?.Errors))
        {
            LogExclusion(bsr, diff, "ParityErrors not in range");
            return false;
        }

        if (filter.ParityWarns && !filter.ParityWarns.InRange(diff.ParitySummary?.Warns))
        {
            LogExclusion(bsr, diff, "ParityWarns not in range");
            return false;
        }

        if (filter.ParityResets && !filter.ParityResets.InRange(diff.ParitySummary?.Resets))
        {
            LogExclusion(bsr, diff, "ParityResets not in range");
            return false;
        }

        if (filter.MaxScore && !filter.MaxScore.InRange(diff.MaxScore))
        {
            LogExclusion(bsr, diff, "MaxScore not in range");
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogExclusion(string bsr, string reason)
    {
        Log.Verbose("Song {Bsr} excluded: {Reason}", bsr, reason);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogExclusion(string bsr, Diff diff, string reason)
    {
        Log.Verbose("Diff {Chara}/{Diff} of Song {Bsr} excluded: {Reason}", diff.Characteristic, diff.Difficulty, bsr,
            reason);
    }
}
