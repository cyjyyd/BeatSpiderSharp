using Newtonsoft.Json;

namespace BeatSpiderSharp.Models.BeatSaver;

public record SongVersion
{
    [JsonProperty("hash")]
    public string? Hash { get; init; }

    [JsonProperty("state")]
    public string? State { get; init; }

    [JsonProperty("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    /**
     * How likely the map is auto generated.
     * Higher is less likely.
     * BeatSage maps are usually negative
     */
    [JsonProperty("sageScore")]
    public int SageScore { get; init; }

    [JsonProperty("diffs")]
    public List<Diff> Diffs { get; init; } = [];

    [JsonProperty("downloadURL")]
    public string? DownloadURL { get; init; }

    [JsonProperty("coverURL")]
    public string? CoverURL { get; init; }

    [JsonProperty("previewURL")]
    public string? PreviewURL { get; init; }

    [JsonProperty("key")]
    public string? Key { get; init; }
}
