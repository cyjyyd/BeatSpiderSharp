using Newtonsoft.Json;

namespace BeatSpiderSharp.Models.BeatSaver;

public record Stats
{
    [JsonProperty("plays")]
    [Obsolete("BeatSaver does not keep track of it")]
    public int Plays { get; init; }

    [JsonProperty("downloads")]
    [Obsolete("BeatSaver does not keep track of it.")]
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
