using Newtonsoft.Json;

namespace BeatSpiderSharp.Models.BeatSaver;

public record ParitySummary
{
    [JsonProperty("errors")]
    public int? Errors { get; init; }

    [JsonProperty("warns")]
    public int? Warns { get; init; }

    [JsonProperty("resets")]
    public int? Resets { get; init; }
}
