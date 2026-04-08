using Newtonsoft.Json;

namespace BeatSpiderSharp.Models.BeatSaver;

public record User
{
    [JsonProperty("id")]
    public int? Id { get; init; }

    [JsonProperty("name")]
    public string? Name { get; init; }

    [JsonProperty("hash")]
    public string? Hash { get; init; }

    [JsonProperty("avatar")]
    public string? Avatar { get; init; }

    [JsonProperty("type")]
    public string? Type { get; init; }

    [JsonProperty("admin")]
    public bool Admin { get; init; }

    [JsonProperty("curator")]
    public bool Curator { get; init; }

    [JsonProperty("seniorCurator")]
    public bool SeniorCurator { get; init; }

    [JsonProperty("playlistUrl")]
    public string? PlaylistUrl { get; init; }

    [JsonProperty("curatorTab")]
    public bool CuratorTab { get; init; }

    [JsonProperty("verifiedMapper")]
    public bool VerifiedMapper { get; init; }

    [JsonProperty("uniqueSet")]
    public bool UniqueSet { get; init; }

    [JsonProperty("suspendedAt")]
    public DateTimeOffset? SuspendedAt { get; init; }
}
