using BeatSpiderSharp.Models.BeatSaver;
using BeatSpiderSharp.Models.Enums;

namespace BeatSpiderSharp.Core.Utilities;

public static class EnumConversions
{
    public static HashSet<MMod> GetMMods(this Diff diff)
    {
        //TODO Vivify
        return Enum.GetValues<MMod>().Where(mod => mod switch
        {
            MMod.NoodleExtensions => diff.Ne,
            MMod.MappingExtensions => diff.Me,
            MMod.Chroma => diff.Chroma,
            MMod.Cinema => diff.Cinema,
            _ => false
        }).ToHashSet();
    }

    public static MDifficulty? GetMDifficulty(this Diff diff)
    {
        return diff.Difficulty switch
        {
            "Easy" => MDifficulty.Easy,
            "Normal" => MDifficulty.Normal,
            "Hard" => MDifficulty.Hard,
            "Expert" => MDifficulty.Expert,
            "ExpertPlus" => MDifficulty.ExpertPlus,
            _ => null
        };
    }

    public static MCharacteristic? GetMCharacteristic(this Diff diff)
    {
        return diff.Characteristic switch
        {
            "Standard" => MCharacteristic.Standard,
            "OneSaber" => MCharacteristic.OneSaber,
            "NoArrows" => MCharacteristic.NoArrows,
            "90Degree" => MCharacteristic.NinetyDegree,
            "360Degree" => MCharacteristic.ThreeSixtyDegree,
            "Lawless" => MCharacteristic.Lawless,
            "Lightshow" => MCharacteristic.Lightshow,
            "Legacy" => MCharacteristic.Other, // TODO
            _ => MCharacteristic.Other
        };
    }
}
