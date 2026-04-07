using BeatSpiderSharp.Models.Enums;

namespace BeatSpiderSharp.Models.Preset.FilterOptions;

public class SongDetailOptions
{
    public IncludeOption<int> UploaderId { get; init; } = new();
    public IncludeOption<string> UploaderName { get; init; } = new(StringComparer.InvariantCultureIgnoreCase);
    public RangeOption<DateTimeOffset> UploadTime { get; init; } = new();
    public LogicIncludeOption<string> IncludeTags { get; init; } = new(StringComparer.InvariantCultureIgnoreCase);
    public LogicExcludeOption<string> ExcludeTags { get; init; } = new(StringComparer.InvariantCultureIgnoreCase);
    public RangeOption<int> UpVotes { get; init; } = new();
    public RangeOption<float> UpVotePercentage { get; init; } = new();
    public RangeOption<int> DownVotes { get; init; } = new();
    public RangeOption<float> DownVotePercentage { get; init; } = new();
    public RangeOption<float> Rating { get; init; } = new();
    public Option<bool> FullSpread { get; init; } = new(true);

    // Doesn't work, BeatSaver do not keep track of download count
    public RangeOption<int> Downloads { get; init; } = new();

    // Doesn't work, BeatSaver do not keep track of play count
    public RangeOption<int> Plays { get; init; } = new();

    // Default to only Human maps
    public Option<bool> AutoMapper { get; init; } = new(false) { Enable = true };
    public RangeOption<float> Bpm { get; init; } = new();
    public RangeOption<int> Duration { get; init; } = new();
    public IncludeOption<RankingStatus> ScoreSaberRanking { get; init; } = new();
    public IncludeOption<RankingStatus> BeatLeaderRanking { get; init; } = new();
    public RangeOption<int> SageScore { get; init; } = new();
    public Option Chinese { get; init; } = new();
}
