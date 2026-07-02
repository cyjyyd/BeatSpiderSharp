using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using BeatSpiderSharp.Core.Interfaces;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Preset.FilterOptions;
using Serilog;

namespace BeatSpiderSharp.Core.Filters;

/// <summary>
///     Filters songs by matching configurable text fields (title, song name, author, mapper, description) against
///     user-supplied search terms. Two term kinds are supported and both run simultaneously; a song passes when
///     <b>any</b> term of either kind matches <b>any</b> enabled haystack field:
///     <list type="bullet">
///         <item>
///             <description>
///                 <b>Regex patterns</b>: each entry in <see cref="SearchOptions.RegexPatterns" /> is compiled as a
///                 case-insensitive .NET regex.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <b>Advance search terms</b>: each <see cref="AdvanceSearchTerm" /> in
///                 <see cref="SearchOptions.AdvanceTerms" />
///                 is a case-insensitive substring match on <see cref="AdvanceSearchTerm.Content" />, with exclusion
///                 words that suppress matches occurring inside them (e.g. <c>cat</c> with exclusions
///                 <c>[category, catalog]</c> matches "cat" but not when it is part of "category" or "catalog").
///             </description>
///         </item>
///     </list>
/// </summary>
public class SearchFilter : ISongFilter
{
    private readonly SearchOptions _options;

    private readonly List<Regex> _regexTerms;

    private readonly List<AdvanceSearchTerm> _advanceTerms;

    public SearchFilter(SearchOptions options)
    {
        _options = options;
        if (!options)
        {
            _regexTerms = [];
            _advanceTerms = [];
            return;
        }

        _regexTerms = options.RegexPatterns
            .Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase))
            .ToList();

        _advanceTerms = options.AdvanceTerms
            .Select(t => new AdvanceSearchTerm
            {
                Content = t.Content.ToLowerInvariant(),
                Exclusions = t.Exclusions.Select(e => e.ToLowerInvariant()).ToList()
            })
            .ToList();
    }

    public bool FilterSong(BeatSpiderSong song)
    {
        if (!_options || (_advanceTerms.Count == 0 && _regexTerms.Count == 0)) return true;
        var map = song.BeatSaverSong;
        var meta = map.Metadata;

        var haystack = new List<string>(5);
        if (_options.SearchTitle && !string.IsNullOrWhiteSpace(map.Name))
        {
            haystack.Add(map.Name.ToLowerInvariant());
        }

        if (_options.SearchSongName)
        {
            if (!string.IsNullOrWhiteSpace(meta?.SongName))
            {
                haystack.Add(meta.SongName.ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(meta?.SongSubName))
            {
                haystack.Add(meta.SongSubName.ToLowerInvariant());
            }
        }

        if (_options.SearchAuthor && !string.IsNullOrWhiteSpace(meta?.SongAuthorName))
        {
            haystack.Add(meta.SongAuthorName.ToLowerInvariant());
        }

        if (_options.SearchMapper && !string.IsNullOrWhiteSpace(meta?.LevelAuthorName))
        {
            haystack.Add(meta.LevelAuthorName.ToLowerInvariant());
        }

        if (_options.SearchDescription && !string.IsNullOrWhiteSpace(map.Description))
        {
            haystack.Add(map.Description.ToLowerInvariant());
        }

        if (haystack.Count == 0) return true;

        var matched = _regexTerms.Any(rx => haystack.Any(rx.IsMatch))
                      || _advanceTerms.Any(term => haystack.Any(text => MatchesAdvance(term, text)));

        if (!matched)
        {
            LogExclusion(song.Bsr, "Search terms not matched");
            return false;
        }

        return true;
    }

    /// <summary>
    ///     Returns true if <paramref name="term" />'s content occurs in <paramref name="lowerText" /> at a position
    ///     that is not covered by any exclusion word. Iterates every occurrence and returns on the first uncovered
    ///     one. Both <paramref name="term" /> and <paramref name="lowerText" /> are expected to be pre-lowercased.
    /// </summary>
    private static bool MatchesAdvance(AdvanceSearchTerm term, string lowerText)
    {
        if (string.IsNullOrWhiteSpace(term.Content)) return false;
        var idx = lowerText.IndexOf(term.Content, StringComparison.Ordinal);
        while (idx >= 0)
        {
            if (!CoveredByExclusion(term, lowerText, idx)) return true;
            idx = lowerText.IndexOf(term.Content, idx + 1, StringComparison.Ordinal);
        }

        return false;
    }

    /// <summary>
    ///     Checks whether a content hit at <paramref name="matchIdx" /> in <paramref name="lowerText" /> sits inside
    ///     any of <paramref name="term" />'s exclusion words. For each exclusion, we compute where the exclusion
    ///     word would have to begin in the haystack for the hit to be embedded in it, then verify the haystack
    ///     actually spells the exclusion at that position.
    /// </summary>
    /// <example>
    ///     haystack = "my category here", content = "cat" matches at index 3.
    ///     Exclusion = "category", "cat" sits at inner index 0 of "category" -> start = 3 - 0 = 3.
    ///     haystack[3..11] == "category" -> the "cat" hit is covered, suppress it.
    /// </example>
    private static bool CoveredByExclusion(AdvanceSearchTerm term, string lowerText, int matchIdx)
    {
        foreach (var excl in term.Exclusions)
        {
            // Where does the content live inside this exclusion word? If it does not, this exclusion cannot
            // possibly cover a content hit, so skip it.
            var inner = excl.IndexOf(term.Content, StringComparison.Ordinal);
            if (inner < 0) continue;

            // Shift the hit position back by that inner offset to get where the exclusion word would start.
            var start = matchIdx - inner;

            // Bounds check: the exclusion word must fit entirely inside the haystack at the computed start.
            if (start < 0 || start + excl.Length > lowerText.Length) continue;

            // Confirm the haystack actually contains the exclusion word at that position.
            // AsSpan + SequenceEqual avoids allocating a substring on this hot path.
            if (lowerText.AsSpan(start, excl.Length).SequenceEqual(excl)) return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void LogExclusion(string bsr, string reason)
    {
        Log.Verbose("Song {Bsr} excluded: {Reason}", bsr, reason);
    }
}
