using Newtonsoft.Json;

namespace BeatSpiderSharp.Models.BeatSaver;

public record Metadata
{
    [JsonProperty("bpm")]
    public float? Bpm { get; init; }

    [JsonProperty("duration")]
    public int? Duration { get; init; }

    [JsonProperty("songName")]
    public string? SongName { get; init; }

    [JsonProperty("songSubName")]
    public string? SongSubName { get; init; }

    [JsonProperty("songAuthorName")]
    public string? SongAuthorName { get; init; }

    [JsonProperty("levelAuthorName")]
    public string? LevelAuthorName { get; init; }
}
