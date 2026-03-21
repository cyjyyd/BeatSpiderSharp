using Newtonsoft.Json;

namespace BeatSpiderSharp.Models.BeatSaver;

public record Song
{
    [JsonProperty("id")]
    public string? Id { get; init; }

    [JsonProperty("name")]
    public string? Name { get; init; }

    [JsonProperty("description")]
    public string? Description { get; init; }

    [JsonProperty("uploader")]
    public User? Uploader { get; init; }

    [JsonProperty("metadata")]
    public Metadata? Metadata { get; init; }

    [JsonProperty("stats")]
    public Stats? Stats { get; init; }

    [JsonProperty("uploaded")]
    public DateTimeOffset? Uploaded { get; init; }

    [JsonProperty("automapper")]
    public bool Automapper { get; init; }

    [JsonProperty("ranked")]
    public bool Ranked { get; init; }

    [JsonProperty("qualified")]
    public bool Qualified { get; init; }

    [JsonProperty("versions")]
    public List<SongVersion> Versions { get; init; } = [];

    public SongVersion LatestVersion => Versions.First();

    [JsonProperty("curator")]
    public User? Curator { get; init; }

    [JsonProperty("curatedAt")]
    public DateTimeOffset? CuratedAt { get; init; }

    [JsonProperty("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

    [JsonProperty("updatedAt")]
    public DateTimeOffset? UpdatedAt { get; init; }

    [JsonProperty("lastPublishedAt")]
    public DateTimeOffset? LastPublishedAt { get; init; }

    [JsonProperty("tags")]
    public List<string> Tags { get; init; } = [];

    [JsonProperty("declaredAi")]
    public string? DeclaredAi { get; init; }

    [JsonProperty("blRanked")]
    public bool BlRanked { get; init; }

    [JsonProperty("blQualified")]
    public bool BlQualified { get; init; }

    [JsonProperty("bookmarked")]
    public bool Bookmarked { get; init; }

    [JsonProperty("nsfw")]
    public bool Nsfw { get; init; }

    [JsonProperty("collaborators")]
    public List<User> Collaborators { get; init; } = [];
}
