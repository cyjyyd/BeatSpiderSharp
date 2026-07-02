namespace BeatSpiderSharp.Models.Preset.FilterOptions;

public class SearchOptions : Option
{
    public List<string> SearchTerms { get; init; } = new();

    public bool RegexSearch { get; set; }

    public bool SearchTitle { get; set; }

    public bool SearchSongName { get; set; }

    public bool SearchAuthor { get; set; }

    public bool SearchMapper { get; set; }

    public bool SearchDescription { get; set; }
}
