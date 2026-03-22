using Newtonsoft.Json;

namespace BeatSpiderSharp.Models.BeatSaver;

public record Stats
{
    [JsonProperty("plays")]
    public int Plays { get; init; }

    [JsonProperty("downloads")]
    public int Downloads { get; init; }

    [JsonProperty("upvotes")]
    public int Upvotes { get; init; }

    [JsonProperty("downvotes")]
    public int Downvotes { get; init; }

    [JsonProperty("score")]
    public float Score { get; init; }

    [JsonProperty("reviews")]
    public int Reviews { get; init; }

    [JsonProperty("sentiment")]
    public string? Sentiment { get; init; }
}
