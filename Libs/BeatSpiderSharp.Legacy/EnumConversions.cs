using BeatSpiderSharp.Models.Enums;

namespace BeatSpiderSharp.Legacy;

//TODO use strings in the preset and remove extra enums 
internal static class EnumConversions
{
    public static ISet<MMod> ToMMods(this LegacyPreset.ModRequirements req)
    {
        var result = new HashSet<MMod>();
        if (req.NoodleExtensions) result.Add(MMod.NoodleExtensions);
        if (req.MappingExtensions) result.Add(MMod.MappingExtensions);
        if (req.Chroma) result.Add(MMod.Chroma);
        if (req.Cinema) result.Add(MMod.Cinema);
        return result;
    }

    public static MCharacteristic ToMCharacteristic(this LegacyPreset.SongFilterSetting.CharacteristicsFilter.SongCharacteristic characteristic)
    {
        return characteristic switch
        {
            LegacyPreset.SongFilterSetting.CharacteristicsFilter.SongCharacteristic.Standard => MCharacteristic.Standard,
            LegacyPreset.SongFilterSetting.CharacteristicsFilter.SongCharacteristic.OneSaber => MCharacteristic.OneSaber,
            LegacyPreset.SongFilterSetting.CharacteristicsFilter.SongCharacteristic.NoArrows => MCharacteristic.NoArrows,
            LegacyPreset.SongFilterSetting.CharacteristicsFilter.SongCharacteristic.NinetyDegree => MCharacteristic.NinetyDegree,
            LegacyPreset.SongFilterSetting.CharacteristicsFilter.SongCharacteristic.ThreeSixtyDegree => MCharacteristic.ThreeSixtyDegree,
            LegacyPreset.SongFilterSetting.CharacteristicsFilter.SongCharacteristic.Lightshow => MCharacteristic.Lightshow,
            LegacyPreset.SongFilterSetting.CharacteristicsFilter.SongCharacteristic.Lawless => MCharacteristic.Lawless,
            _ => throw new ArgumentOutOfRangeException(nameof(characteristic), characteristic, "Invalid characteristic")
        };
    }

    public static MDifficulty ToMDifficulty(this LegacyPreset.SongFilterSetting.DifficultyFilter.Difficulty difficulty)
    {
        return difficulty switch
        {
            LegacyPreset.SongFilterSetting.DifficultyFilter.Difficulty.Easy => MDifficulty.Easy,
            LegacyPreset.SongFilterSetting.DifficultyFilter.Difficulty.Normal => MDifficulty.Normal,
            LegacyPreset.SongFilterSetting.DifficultyFilter.Difficulty.Hard => MDifficulty.Hard,
            LegacyPreset.SongFilterSetting.DifficultyFilter.Difficulty.Expert => MDifficulty.Expert,
            LegacyPreset.SongFilterSetting.DifficultyFilter.Difficulty.ExpertPlus => MDifficulty.ExpertPlus,
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, "Invalid  difficulty")
        };
    }
}
