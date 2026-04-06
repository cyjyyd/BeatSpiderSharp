using BeatSpiderSharp.Models.Enums;

namespace BeatSpiderSharp.Models.Preset.FilterOptions;

//TODO separate song and individual difficulty filters
public class DetailOptions
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
    public LogicIncludeOption<MCharacteristic> IncludeCharacteristics { get; init; } = new();
    public LogicIncludeOption<MDifficulty> IncludeDifficulties { get; init; } = new();
    public LogicIncludeOption<MMod> RequireMods { get; init; } = new();
    public ExcludeOption<MMod> ExcludeMods { get; init; } = new();

    // Doesn't work, BeatSaver do not keep track of download count
    public RangeOption<int> Downloads { get; init; } = new();

    // Doesn't work, BeatSaver do not keep track of play count
    public RangeOption<int> Plays { get; init; } = new();

    // Default to only Human maps
    public Option<bool> AutoMapper { get; init; } = new(false) { Enable = true };
    public RangeOption<float> Bpm { get; init; } = new();
    public RangeOption<int> Duration { get; init; } = new();
    public RangeOption<float> Seconds { get; init; } = new();
    public RangeOption<float> Beats { get; init; } = new();
    public RangeOption<float> Njs { get; init; } = new();
    public RangeOption<float> Offset { get; init; } = new();
    public RangeOption<float> Nps { get; init; } = new();
    public RangeOption<int> Notes { get; init; } = new();
    public RangeOption<int> Bombs { get; init; } = new();
    public RangeOption<int> Events { get; init; } = new();
    public RangeOption<int> Walls { get; init; } = new();
    public IncludeOption<RankingStatus> ScoreSaberRanking { get; init; } = new();
    public IncludeOption<RankingStatus> BeatLeaderRanking { get; init; } = new();
    public RangeOption<float> ScoreSaberStars { get; init; } = new();
    public RangeOption<float> BeatLeaderStars { get; init; } = new();
    public RangeOption<int> ParityErrors { get; init; } = new();
    public RangeOption<int> ParityWarns { get; init; } = new();
    public RangeOption<int> ParityResets { get; init; } = new();
    public RangeOption<int> SageScore { get; init; } = new();
    public RangeOption<int> MaxScore { get; init; } = new();
    public Option Chinese { get; init; } = new();
}
