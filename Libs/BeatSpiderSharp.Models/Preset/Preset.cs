namespace BeatSpiderSharp.Models.Preset;

public class Preset
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;

    public InputConfig Input { get; init; } = new();

    public OutputConfig Output { get; init; } = new();

    /// <summary>
    /// Multiple instances applied as OR
    /// </summary>
    public IList<FilterConfig> FilterOptions { get; init; } = new List<FilterConfig>(1);
}
