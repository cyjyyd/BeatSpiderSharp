namespace BeatSpiderSharp.Models.Preset.FilterOptions;

public class SearchOptions : Option
{
    public List<string> RegexPatterns { get; init; } = [];

    public List<AdvanceSearchTerm> AdvanceTerms { get; init; } = [];

    public bool SearchTitle { get; set; }

    public bool SearchSongName { get; set; }

    public bool SearchAuthor { get; set; }

    public bool SearchMapper { get; set; }

    public bool SearchDescription { get; set; }
}

public class AdvanceSearchTerm
{
    public required string Content { get; set; }

    public List<string> Exclusions { get; init; } = [];
}
