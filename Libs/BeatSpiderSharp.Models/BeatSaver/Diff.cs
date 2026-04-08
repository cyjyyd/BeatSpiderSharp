using Newtonsoft.Json;

namespace BeatSpiderSharp.Models.BeatSaver;

public record Diff
{
    [JsonProperty("njs")]
    public float Njs { get; init; }

    [JsonProperty("offset")]
    public float Offset { get; init; }

    [JsonProperty("notes")]
    public int Notes { get; init; }

    [JsonProperty("bombs")]
    public int Bombs { get; init; }

    [JsonProperty("obstacles")]
    public int Obstacles { get; init; }

    [JsonProperty("nps")]
    public float Nps { get; init; }

    /**
     * The length of the map in beats.
     */
    [JsonProperty("length")]
    public float Length { get; init; }

    [JsonProperty("characteristic")]
    public string? Characteristic { get; init; }

    [JsonProperty("difficulty")]
    public string? Difficulty { get; init; }

    [JsonProperty("events")]
    public int Events { get; init; }

    [JsonProperty("chroma")]
    public bool Chroma { get; init; }

    [JsonProperty("me")]
    public bool Me { get; init; }

    [JsonProperty("ne")]
    public bool Ne { get; init; }

    [JsonProperty("cinema")]
    public bool Cinema { get; init; }

    [JsonProperty("seconds")]
    public float Seconds { get; init; }

    [JsonProperty("paritySummary")]
    public ParitySummary? ParitySummary { get; init; }

    [JsonProperty("stars")]
    public float Stars { get; init; }

    [JsonProperty("maxScore")]
    public int MaxScore { get; init; }

    [JsonProperty("label")]
    public string? Label { get; init; }

    [JsonProperty("blStars")]
    public float BlStars { get; init; }

    [JsonProperty("environment")]
    public string? Environment { get; init; }

    [JsonProperty("vivify")]
    public bool Vivify { get; init; }
}
