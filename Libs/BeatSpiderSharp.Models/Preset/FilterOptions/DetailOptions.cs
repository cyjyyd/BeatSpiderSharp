using BeatSpiderSharp.Models.Enums;

namespace BeatSpiderSharp.Models.Preset.FilterOptions;

//TODO separate song and individual difficulty filters
public class DetailOptions
{
    public IncludeOption<int> UploaderId { get; set; } = new();
    public IncludeOption<string> UploaderName { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
    public RangeOption<DateTimeOffset> UploadTime { get; set; } = new();
    public LogicIncludeOption<string> IncludeTags { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
    public LogicExcludeOption<string> ExcludeTags { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);
    public RangeOption<int> UpVotes { get; set; } = new();
    public RangeOption<float> UpVotePercentage { get; set; } = new();
    public RangeOption<int> DownVotes { get; set; } = new();
    public RangeOption<float> DownVotePercentage { get; set; } = new();
    public RangeOption<float> Rating { get; set; } = new();
    public Option<bool> FullSpread { get; set; } = new(true);
    public LogicIncludeOption<MCharacteristic> IncludeCharacteristics { get; set; } = new();
    public LogicIncludeOption<MDifficulty> IncludeDifficulties { get; set; } = new();
    public LogicIncludeOption<MMod> RequireMods { get; set; } = new();
    public ExcludeOption<MMod> ExcludeMods { get; set; } = new();

    // Doesn't work, BeatSaver do not keep track of download count
    public RangeOption<int> Downloads { get; set; } = new();

    // Doesn't work, BeatSaver do not keep track of play count
    public RangeOption<int> Plays { get; set; } = new();

    // Default to only Human maps
    public Option<bool> AutoMapper { get; set; } = new(false) { Enable = true };
    public RangeOption<float> Bpm { get; set; } = new();
    public RangeOption<int> Duration { get; set; } = new();
    public RangeOption<float> Seconds { get; set; } = new();
    public RangeOption<float> Beats { get; set; } = new();
    public RangeOption<float> Njs { get; set; } = new();
    public RangeOption<float> Offset { get; set; } = new();
    public RangeOption<float> Nps { get; set; } = new();
    public RangeOption<int> Notes { get; set; } = new();
    public RangeOption<int> Bombs { get; set; } = new();
    public RangeOption<int> Events { get; set; } = new();
    public RangeOption<int> Walls { get; set; } = new();
    public IncludeOption<RankingStatus> ScoreSaberRanking { get; set; } = new();
    public IncludeOption<RankingStatus> BeatLeaderRanking { get; set; } = new();
    public RangeOption<float> ScoreSaberStars { get; set; } = new();
    public RangeOption<float> BeatLeaderStars { get; set; } = new();
    public RangeOption<int> ParityErrors { get; set; } = new();
    public RangeOption<int> ParityWarns { get; set; } = new();
    public RangeOption<int> ParityResets { get; set; } = new();
    public RangeOption<int> SageScore { get; set; } = new();
    public RangeOption<int> MaxScore { get; set; } = new();
    public Option Chinese { get; set; } = new();
}
