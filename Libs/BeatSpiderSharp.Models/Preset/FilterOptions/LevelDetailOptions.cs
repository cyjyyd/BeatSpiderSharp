using BeatSpiderSharp.Models.Enums;

namespace BeatSpiderSharp.Models.Preset.FilterOptions;

public class LevelDetailOptions
{
    public LogicIncludeOption<MCharacteristic> IncludeCharacteristics { get; init; } = new();
    public LogicIncludeOption<MDifficulty> IncludeDifficulties { get; init; } = new();
    public LogicIncludeOption<MMod> RequireMods { get; init; } = new();
    public ExcludeOption<MMod> ExcludeMods { get; init; } = new();
    public RangeOption<float> Seconds { get; init; } = new();
    public RangeOption<float> Beats { get; init; } = new();
    public RangeOption<float> Njs { get; init; } = new();
    public RangeOption<float> Offset { get; init; } = new();
    public RangeOption<float> Nps { get; init; } = new();
    public RangeOption<int> Notes { get; init; } = new();
    public RangeOption<int> Bombs { get; init; } = new();
    public RangeOption<int> Events { get; init; } = new();
    public RangeOption<int> Walls { get; init; } = new();
    public RangeOption<float> ScoreSaberStars { get; init; } = new();
    public RangeOption<float> BeatLeaderStars { get; init; } = new();
    public RangeOption<int> ParityErrors { get; init; } = new();
    public RangeOption<int> ParityWarns { get; init; } = new();
    public RangeOption<int> ParityResets { get; init; } = new();
    public RangeOption<int> MaxScore { get; init; } = new();
};
