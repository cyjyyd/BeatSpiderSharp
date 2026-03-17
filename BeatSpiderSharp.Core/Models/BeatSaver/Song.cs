using Newtonsoft.Json;

namespace BeatSpiderSharp.Core.Models.BeatSaver;

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

public record User
{
    [JsonProperty("id")]
    public int Id { get; init; }

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

public record Metadata
{
    [JsonProperty("bpm")]
    public float Bpm { get; init; }

    [JsonProperty("duration")]
    public int Duration { get; init; }

    [JsonProperty("songName")]
    public string? SongName { get; init; }

    [JsonProperty("songSubName")]
    public string? SongSubName { get; init; }

    [JsonProperty("songAuthorName")]
    public string? SongAuthorName { get; init; }

    [JsonProperty("levelAuthorName")]
    public string? LevelAuthorName { get; init; }
}

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

public record SongVersion
{
    [JsonProperty("hash")]
    public string? Hash { get; init; }

    [JsonProperty("state")]
    public string? State { get; init; }

    [JsonProperty("createdAt")]
    public DateTimeOffset? CreatedAt { get; init; }

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

public record ParitySummary
{
    [JsonProperty("errors")]
    public int Errors { get; init; }

    [JsonProperty("warns")]
    public int Warns { get; init; }

    [JsonProperty("resets")]
    public int Resets { get; init; }
}
