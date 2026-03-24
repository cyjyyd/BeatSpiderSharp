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
    public RangeOption<float> Bpm { get; set; } = new();
    public RangeOption<int> Duration { get; set; } = new();
    public RangeOption<float> Njs { get; set; } = new();
    public RangeOption<float> Nps { get; set; } = new();
    public RangeOption<int> Notes { get; set; } = new();
    public RangeOption<int> Bombs { get; set; } = new();
    public RangeOption<int> Walls { get; set; } = new();
    public IncludeOption<RankingStatus> ScoreSaberRanking { get; set; } = new();
    public IncludeOption<RankingStatus> BeatLeaderRanking { get; set; } = new();
    public RangeOption<float> ScoreSaberStars { get; set; } = new();
    public RangeOption<float> BeatLeaderStars { get; set; } = new();
    public Option Chinese { get; set; } = new();
}
