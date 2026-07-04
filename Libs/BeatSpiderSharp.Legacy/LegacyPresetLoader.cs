using System.Diagnostics;
using System.Text.RegularExpressions;
using BeatSpiderSharp.Extensions;
using BeatSpiderSharp.Models;
using BeatSpiderSharp.Models.Enums;
using BeatSpiderSharp.Models.Preset;
using BeatSpiderSharp.Models.Preset.FilterOptions;
using Newtonsoft.Json;
using Serilog;

namespace BeatSpiderSharp.Legacy;

public static partial class LegacyPresetLoader
{
    //beatsaver.com/profile/58338
    [GeneratedRegex(@"beatsaver\.com/profile/(\d+)")]
    private static partial Regex MapperUrlRegex();

    private static readonly JsonSerializer LegacySerializer = JsonSerializer.Create(new JsonSerializerSettings
    {
        Formatting = Formatting.Indented,
#if DEBUG
        MissingMemberHandling = MissingMemberHandling.Error
#endif
    });

    public static Preset? LoadAndConvertLegacyPreset(string path, string author)
    {
        var legacy = LoadLegacyPreset(path);
        if (legacy is null)
        {
            return null;
        }
#if DEBUG
        SaveLegacyPreset(legacy, $"./{Path.GetFileName(path)}.legacy.saved.json");
#endif
        try
        {
            var preset = ConvertToPreset(legacy, Path.GetFileNameWithoutExtension(path), author);
            return preset;
        }
        catch (Exception e) when (e is not LegacyConversionException)
        {
            Log.Error(e, "Unexpected exception when converting legacy preset");
            throw new LegacyConversionException("Failed to convert legacy preset", innerException: e);
        }
    }

    internal static LegacyPreset? LoadLegacyPreset(string path)
    {
        Log.Information("Loading legacy preset from {Path}", path);
        return LegacySerializer.DeserializeObject<LegacyPreset>(path);
    }

    internal static void SaveLegacyPreset(LegacyPreset preset, string path)
    {
        Log.Information("Writing legacy preset to {Path}", path);
        LegacySerializer.Serialize(preset, path);
    }

    internal static Preset ConvertToPreset(LegacyPreset legacyPreset, string fileName, string author)
    {
        Log.Information("Converting legacy preset to new preset: {Name}", fileName);
        WarnUnsupported(legacyPreset);
        var songOptions = ConvertSongFilterOptions(legacyPreset.SongFilter);
        var levelOptions = ConvertLevelFilterOptions(legacyPreset.SongFilter);
        var searchOptions = ConvertSearchFilterOptions(legacyPreset.SearchFilter);
        var output = new OutputConfig
        {
            LimitSongs = legacyPreset.Limits.Count.Enable,
            MaxSongs = legacyPreset.Limits.Count.Content,
            Playlist = new PlaylistConfig
            {
                SavePlaylist = legacyPreset.Output.Playlist.Enable,
                PostProcessPlaylist = true,
                PlaylistDirectory = legacyPreset.Output.Playlist.Path,
                FileNameTemplate = GetPlaylistFileTemplate(fileName)
            },
            SongDownload = new SongDownloadConfig
            {
                DownloadSongs = legacyPreset.Output.Songs.Enable,
                DownloadPath = legacyPreset.Output.Songs.Path,
                FolderNameTemplate = ConvertSongFolderTemplate(legacyPreset.Output.Naming.Template),
                EnglishOnly = legacyPreset.Output.Naming.AllEnglish,
                SkipExisting = true,
                ExistingSongPaths = legacyPreset.LocalSong.LocalSongSkipPaths.ToList(),
                CopyLocalSongs = true,
                LocalSongPaths = legacyPreset.LocalSong.LocalSongPaths.ToList()
            }
        };
        var input = new InputConfig
        {
            Source = SongInputSource.BeatSaver,
            Playlists = string.IsNullOrWhiteSpace(legacyPreset.PlaylistInput.Path)
                ? []
                : [legacyPreset.PlaylistInput.Path],
            ManualInput = legacyPreset.ManualSongInput.Songs.ToList()
        };
        var filterConfig = new FilterConfig
        {
            SongDetailFilter = songOptions,
            LevelDetailOptions = levelOptions,
            SearchOptions = searchOptions
        };
        Log.Debug("Legacy preset input source: {Source}", legacyPreset.SongSource);
        switch (legacyPreset.SongSource)
        {
            case LegacyPreset.DataSource.LocalCache:
                input.Source = SongInputSource.BeatSaver;
                break;
            case LegacyPreset.DataSource.Playlist:
                input.Source = SongInputSource.Playlists;
                break;
            case LegacyPreset.DataSource.ManualInput:
                input.Source = SongInputSource.ManualInput;
                break;
            case LegacyPreset.DataSource.BeastSaber:
                throw new LegacyConversionException("BeastSaber source is not supported", "Input Source");
            case LegacyPreset.DataSource.Mapper:
                MergeMapperSetting(filterConfig, legacyPreset.Mapper);
                input.Source = SongInputSource.BeatSaver;
                break;
            case LegacyPreset.DataSource.ScoreSaber:
                MergeScoreSaberSetting(filterConfig, legacyPreset.ScoreSaber);
                input.Source = SongInputSource.BeatSaver;
                break;
            case LegacyPreset.DataSource.BeatSaver:
                MergeBeatSaverSetting(filterConfig, legacyPreset.BeatSaver);
                input.Source = SongInputSource.BeatSaver;
                output.SortType = legacyPreset.BeatSaver.Sort switch
                {
                    LegacyPreset.BeatSaverSetting.SortType.Latest => SortType.Latest,
                    LegacyPreset.BeatSaverSetting.SortType.Rating => SortType.Rating,
                    _ => throw new LegacyConversionException(
                        $"Only {nameof(LegacyPreset.BeatSaverSetting.SortType.Latest)} and {nameof(LegacyPreset.BeatSaverSetting.SortType.Rating)} BeatSaver search sort types are supported",
                        "BeatSaver Search")
                };
                break;
            default:
                throw new LegacyConversionException(
                    $"Unknown song source from legacy preset: {legacyPreset.SongSource}", "Input Source");
        }

        var name = GetPresetName(fileName);
        Log.Information("Using preset name: {Name}", name);
        var preset = new Preset
        {
            Name = name,
            Author = author,
            Description = $"该歌单由免费工具 BeatSpider (BeatSpiderSharp) 生成。\n\n" +
                          $"源项目地址（已停止更新）：https://github.com/WGzeyu/BeatSpider\n" +
                          $"重制版项目地址：https://github.com/qe201020335/BeatSpiderSharp",
            Input = input,
            Output = output,
            FilterOptions = [filterConfig]
        };

#if DEBUG
        Log.Verbose("Legacy preset: {@LegacyPreset}", legacyPreset);
        Log.Verbose("Converted preset: {@NewPreset}", preset);
#endif
        return preset;
    }

    private static string GetPlaylistFileTemplate(string fileName) => fileName
        .Replace(LegacyTemplates.DATE, Templates.DATE, StringComparison.OrdinalIgnoreCase);

    private static string ConvertSongFolderTemplate(string template) => template
        .Replace(LegacyTemplates.BSR, Templates.BSR, StringComparison.OrdinalIgnoreCase)
        .Replace(LegacyTemplates.HASH, Templates.HASH, StringComparison.OrdinalIgnoreCase)
        .Replace(LegacyTemplates.TITLE, Templates.TITLE, StringComparison.OrdinalIgnoreCase)
        .Replace(LegacyTemplates.SONG_NAME, Templates.SONG_NAME, StringComparison.OrdinalIgnoreCase)
        .Replace(LegacyTemplates.SONG_SUB_NAME, Templates.SONG_SUB_NAME, StringComparison.OrdinalIgnoreCase)
        .Replace(LegacyTemplates.SONG_AUTHOR, Templates.SONG_AUTHOR, StringComparison.OrdinalIgnoreCase)
        .Replace(LegacyTemplates.MAPPER, Templates.MAPPER, StringComparison.OrdinalIgnoreCase);

    private static string GetPresetName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            // unlikely to happen
            return "No name " + DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        var leftBracket = fileName.IndexOf('【');
        if (leftBracket == -1)
        {
            return fileName;
        }

        var rightBracket = fileName[leftBracket..].IndexOf('】');
        if (rightBracket == -1)
        {
            return fileName;
        }

        var name = fileName[(leftBracket + 1)..(rightBracket + leftBracket)];
        return string.IsNullOrWhiteSpace(name) ? fileName : name;
    }

    private static void WarnUnsupported(LegacyPreset preset)
    {
        if (preset.ThumbnailTag.Enable.Enable)
        {
            throw new LegacyConversionException("Thumbnail Tag is not supported");
        }
    }

    //TODO Unit tests
    private static void CombineRange<T>(RangeOption<T> o, LegacyPreset.IMinMaxSetting<T> s, string name)
        where T : struct, IComparable<T>
    {
        var min = s.Min;
        var max = s.Max;
        if (o.Enable)
        {
            var oMin = o.Min;
            var oMax = o.Max;
            //merge the range values
            min = oMin.HasValue && min.HasValue ? oMin.Value.CompareTo(min.Value) > 0 ? oMin : min : oMin ?? min;
            max = oMax.HasValue && max.HasValue ? oMax.Value.CompareTo(max.Value) < 0 ? oMax : max : oMax ?? max;
        }

        if (min.HasValue && max.HasValue && min.Value.CompareTo(max.Value) > 0)
        {
            Log.Error("Invalid range with min greater than max. Min: {Min}, Max: {Max}", min.Value, max.Value);
            throw new LegacyConversionException("Invalid range with min greater than max.", name);
        }

        o.Enable = true;
        o.Min = min;
        o.Max = max;
    }

    private static void MergeBeatSaverSetting(FilterConfig filterConfig, LegacyPreset.BeatSaverSetting setting)
    {
        var songOptions = filterConfig.SongDetailFilter;
        var levelOptions = filterConfig.LevelDetailOptions;
        Log.Debug("Merging BeatSaver source settings into filter options");
        if (!string.IsNullOrWhiteSpace(setting.SearchKeyword) || setting.StartPage.HasValue)
        {
            //TODO add search keyword to search filter settings once search filter is supported
            throw new LegacyConversionException("BeatSaver search keyword and starting page are not supported",
                "BeatSaver Search");
        }

        // following BeatSaver API's handling
        var (autoMapperEnable, autoMapperFilter) = setting.AutoMapper switch
        {
            { Enable: false } => (true, false), // no auto mapped 
            { AutoMapper: true } => (false, false), // all maps
            { AutoMapper: false } => (true, true) // only auto mapped
        };

        if (songOptions.AutoMapper.Enable && autoMapperEnable && songOptions.AutoMapper.Filter != autoMapperFilter)
        {
            throw new LegacyConversionException(
                "Conflicting auto mapper settings from BeatSaver search setting and song filter setting",
                "AutoMapper");
        }

        if (autoMapperEnable)
        {
            songOptions.AutoMapper.Enable = true;
            songOptions.AutoMapper.Filter = autoMapperFilter;
        }

        if (setting.RankedSong.Enable)
        {
            var states = setting.RankedSong.IsRanked
                ? new HashSet<RankingStatus> { RankingStatus.Ranked }
                : new HashSet<RankingStatus> { RankingStatus.Unranked, RankingStatus.Qualified };
            if (songOptions.ScoreSaberRanking is { Enable: true, Filter.Count: > 0 })
            {
                states.IntersectWith(songOptions.ScoreSaberRanking.Filter);
                if (states.Count == 0)
                {
                    Log.Error(
                        "Conflicting ScoreSaber ranked filter. BeatSaver input setting and song filter has no overlap for ScoreSaber ranking status");
                    throw new LegacyConversionException(
                        "Conflicting ScoreSaber ranked filter with BeatSaver input setting", "ScoreSaberRanking");
                }
            }

            songOptions.ScoreSaberRanking.Enable = true;
            songOptions.ScoreSaberRanking.Filter.ReplaceWith(states);
        }

        songOptions.FullSpread.Enable = setting.Difficulty.Enable;
        songOptions.FullSpread.Filter = setting.Difficulty.IsFullSpread;

        if (setting.Bpm.Enable)
        {
            Log.Debug("Merging BeatSaver BPM setting into filter options");
            CombineRange(songOptions.Bpm, setting.Bpm, "Bpm");
        }

        if (setting.Nps.Enable)
        {
            Log.Information("Merging BeatSaver NPS setting into filter options");
            CombineRange(levelOptions.Nps, setting.Nps, "Nps");
        }

        if (setting.Duration.Enable)
        {
            Log.Information("Merging BeatSaver duration setting into filter options");
            CombineRange(songOptions.Duration, setting.Duration, "Duration");
        }

        if (setting.UploadTime.Enable)
        {
            Log.Information("Merging BeatSaver upload time setting into filter options");
            CombineRange(songOptions.UploadTime, setting.UploadTime, "UploadTime");
        }

        if (setting.Rating.Enable)
        {
            Log.Information("Merging BeatSaver rating setting into filter options");
            CombineRange(songOptions.Rating, setting.Rating, "Rating");
        }

        // mods related is a bit tricky
        var requireMods = setting.RequireMods.RequireMods.ToMMods();
        var excludeMods = setting.ExcludeMods.ExcludeMods.ToMMods();
        if (setting.RequireMods.Enable && setting.ExcludeMods.Enable)
        {
            if (requireMods.Intersect(excludeMods).Any())
            {
                Log.Error(
                    "Conflicting mods requirement. BeatSaver input setting has overlap for required and excluded mods");
                throw new LegacyConversionException("Conflicting mods requirement in BeatSaver input setting",
                    "Required/Excluded Mods");
            }
        }

        if (setting.RequireMods.Enable && requireMods.Count > 0)
        {
            // BeatSaver tests the values as OR
            if (levelOptions.RequireMods && levelOptions.RequireMods.Filter.Count > 0)
            {
                //Original project only tests this as or
                Debug.Assert(levelOptions.RequireMods.IsOr);
                if (!levelOptions.RequireMods.Filter.SetEquals(requireMods))
                {
                    // to achieve this, it needs to AND the results of two OR operations
                    Log.Error(
                        "Unsupported mods requirement combination between BeatSaver input setting and song filter setting");
                    throw new LegacyConversionException(
                        "Unsupported mods requirement combination between BeatSaver input setting and song filter setting",
                        "Required Mods");
                }
            }
            else
            {
                levelOptions.RequireMods.Enable = true;
                levelOptions.RequireMods.IsOr = true;
                levelOptions.RequireMods.Filter.Clear();
                levelOptions.RequireMods.Filter.UnionWith(requireMods);
            }
        }

        if (setting.ExcludeMods.Enable && excludeMods.Count > 0)
        {
            // BeatSaver tests the values in a very weird way.
            // If a map doesn't require at least one of the excluded mods, it passes the filter.
            // As a result, a filter excluding all mods will consider a map requiring one mod satisfying the condition.
            if (excludeMods.Count != 1)
            {
                // no support for this ridiculous logic
                Log.Error("Unsupported BeatSaver excluded mods setting");
                throw new LegacyConversionException("Unsupported BeatSaver excluded mods setting", "Excluded Mods");
            }

            if (levelOptions.ExcludeMods)
            {
                levelOptions.ExcludeMods.Filter.Add(excludeMods.First());
            }
            else
            {
                levelOptions.ExcludeMods.Enable = true;
                levelOptions.ExcludeMods.Filter.Clear();
                levelOptions.ExcludeMods.Filter.Add(excludeMods.First());
            }
        }
    }

    private static void MergeScoreSaberSetting(FilterConfig filterConfig, LegacyPreset.ScoreSaberSetting setting)
    {
        var songOptions = filterConfig.SongDetailFilter;
        var levelOptions = filterConfig.LevelDetailOptions;
        Log.Debug("Merging ScoreSaber source setting into filter options");
        if (songOptions.ScoreSaberRanking && !songOptions.ScoreSaberRanking.SatisfiedBy(RankingStatus.Ranked))
        {
            Log.Error("Conflicting ScoreSaber ranking status. Existing filter setting does not include {Status}",
                RankingStatus.Ranked);
            throw new LegacyConversionException("Conflicting ScoreSaber ranking status", "ScoreSaberRanking");
        }

        songOptions.ScoreSaberRanking.Enable = true;
        songOptions.ScoreSaberRanking.Filter.Clear();
        songOptions.ScoreSaberRanking.Filter.Add(RankingStatus.Ranked);
        CombineRange(levelOptions.ScoreSaberStars, setting.StarDifficulty, "ScoreSaber stars");
    }

    private static void MergeMapperSetting(FilterConfig filterConfig, LegacyPreset.MapperSetting setting)
    {
        var options = filterConfig.SongDetailFilter;
        var url = setting.MapperAddress;
        Log.Debug("Extracting uploader id from mapper url: {Url}", url);
        var match = MapperUrlRegex().Match(url);
        if (match is { Success: true, Groups.Count: 2 } && int.TryParse(match.Groups[1].Value, out var id))
        {
            Log.Information("Found uploader id from mapper url: {Url}", id);
            if (options.UploaderId && !options.UploaderId.SatisfiedBy(id))
            {
                Log.Error("Conflicting uploader id. '{Id1}' from url is not included in filter setting {Id2}", id,
                    options.UploaderId.Filter);
                throw new LegacyConversionException("Conflicting uploader id from url and song filter setting",
                    "Mapper Url");
            }

            options.UploaderId.Enable = true;
            options.UploaderId.Filter.Clear();
            options.UploaderId.Filter.Add(id);
        }
        else
        {
            Log.Error("Failed to find uploader id from mapper url: {Url}", url);
            throw new LegacyConversionException("Cannot find uploader id from mapper url", "Mapper Url");
        }
    }

    private static SongDetailOptions ConvertSongFilterOptions(LegacyPreset.SongFilterSetting setting) => new()
    {
        UploaderId = new IncludeOption<int>
        {
            Enable = setting.UploaderIds.Enable,
            Filter = setting.UploaderIds.Content.ToHashSet()
        },
        UploaderName = new IncludeOption<string>
        {
            Enable = setting.UploaderNames.Enable,
            Filter = setting.UploaderNames.Content.ToHashSet(StringComparer.InvariantCultureIgnoreCase)
        },
        Downloads = new RangeOption<int>
        {
            Enable = setting.DownloadCount.Enable,
            Min = setting.DownloadCount.Min,
            Max = setting.DownloadCount.Max
        },
        Plays = new RangeOption<int>
        {
            Enable = setting.PlayCount.Enable,
            Min = setting.PlayCount.Min,
            Max = setting.PlayCount.Max
        },
        UpVotes = new RangeOption<int>
        {
            Enable = setting.UpVotes.Enable,
            Min = setting.UpVotes.Min,
            Max = setting.UpVotes.Max
        },
        UpVotePercentage = new RangeOption<float>
        {
            Enable = setting.UpVotePercentage.Enable,
            Min = setting.UpVotePercentage.Min,
            Max = setting.UpVotePercentage.Max
        },
        DownVotes = new RangeOption<int>
        {
            Enable = setting.DownVotes.Enable,
            Min = setting.DownVotes.Min,
            Max = setting.DownVotes.Max
        },
        DownVotePercentage = new RangeOption<float>
        {
            Enable = setting.DownVotePercentage.Enable,
            Min = setting.DownVotePercentage.Min,
            Max = setting.DownVotePercentage.Max
        },
        Rating = new RangeOption<float>
        {
            Enable = setting.Rating.Enable,
            Min = setting.Rating.Min,
            Max = setting.Rating.Max
        },
        AutoMapper = new Option<bool>(setting.AutoMapper.AutoMapper)
        {
            Enable = setting.AutoMapper.Enable
        },
        ScoreSaberRanking = new IncludeOption<RankingStatus>
        {
            Enable = setting.RankedSong.Enable,
            Filter = setting.RankedSong.IsRanked
                ? new HashSet<RankingStatus> { RankingStatus.Ranked }
                : new HashSet<RankingStatus> { RankingStatus.Unranked, RankingStatus.Qualified }
        },
        Chinese = new Option
        {
            Enable = setting.FilterChinese.Enable
        },
        Bpm = new RangeOption<float>
        {
            Enable = setting.Bpm.Enable,
            Min = setting.Bpm.Min,
            Max = setting.Bpm.Max
        },
        Duration = new RangeOption<int>
        {
            Enable = setting.Duration.Enable,
            Min = setting.Duration.Min,
            Max = setting.Duration.Max
        },

        UploadTime = new RangeOption<DateTimeOffset>
        {
            Enable = setting.UploadTime.Enable,
            Min = setting.UploadTime.Min,
            Max = setting.UploadTime.Max
        },
        IncludeTags = new LogicIncludeOption<string>
        {
            Enable = setting.Tags.Include.Enable,
            Filter = setting.Tags.Include.Content.ToHashSet(StringComparer.InvariantCultureIgnoreCase),
            IsOr = !setting.Tags.Include.And
        },
        ExcludeTags = new LogicExcludeOption<string>
        {
            Enable = setting.Tags.Exclude.Enable,
            Filter = setting.Tags.Exclude.Content.ToHashSet(StringComparer.InvariantCultureIgnoreCase),
            IsOr = !setting.Tags.Exclude.And
        },
        SageScore = new RangeOption<int>
        {
            Enable = setting.SageScore.Enable,
            Min = setting.SageScore.Min,
            Max = setting.SageScore.Max
        }
    };

    private static LevelDetailOptions ConvertLevelFilterOptions(LegacyPreset.SongFilterSetting setting) => new()
    {
        RequireMods = new LogicIncludeOption<MMod>
        {
            Enable = setting.RequireMods.Enable,
            Filter = setting.RequireMods.Mods.ToMMods(),
            IsOr = true
        },
        ExcludeMods = new ExcludeOption<MMod>
        {
            Enable = setting.ExcludeMods.Enable,
            Filter = setting.ExcludeMods.Mods.ToMMods()
        },
        IncludeCharacteristics = new LogicIncludeOption<MCharacteristic>
        {
            Enable = setting.IncludeCharacteristics.Enable,
            Filter = setting.IncludeCharacteristics.Characteristics.Select(EnumConversions.ToMCharacteristic)
                .ToHashSet(),
            IsOr = true
        },
        IncludeDifficulties = new LogicIncludeOption<MDifficulty>
        {
            Enable = setting.IncludeDifficulties.Enable,
            Filter = setting.IncludeDifficulties.Difficulties.Select(EnumConversions.ToMDifficulty).ToHashSet(),
            IsOr = !setting.IncludeDifficulties.And
        },
        Seconds = new RangeOption<float>
        {
            Enable = setting.MapSeconds.Enable,
            Min = setting.MapSeconds.Min,
            Max = setting.MapSeconds.Max
        },
        Beats = new RangeOption<float>
        {
            Enable = setting.MapLength.Enable,
            Min = setting.MapLength.Min,
            Max = setting.MapLength.Max
        },
        Njs = new RangeOption<float>
        {
            Enable = setting.Njs.Enable,
            Min = setting.Njs.Min,
            Max = setting.Njs.Max
        },
        Offset = new RangeOption<float>
        {
            Enable = setting.Offset.Enable,
            Min = setting.Offset.Min,
            Max = setting.Offset.Max
        },
        Notes = new RangeOption<int>
        {
            Enable = setting.Notes.Enable,
            Min = setting.Notes.Min,
            Max = setting.Notes.Max
        },
        Nps = new RangeOption<float>
        {
            Enable = setting.Nps.Enable,
            Min = setting.Nps.Min,
            Max = setting.Nps.Max
        },
        Bombs = new RangeOption<int>
        {
            Enable = setting.Bombs.Enable,
            Min = setting.Bombs.Min,
            Max = setting.Bombs.Max
        },
        Events = new RangeOption<int>
        {
            Enable = setting.Events.Enable,
            Min = setting.Events.Min,
            Max = setting.Events.Max
        },
        Walls = new RangeOption<int>
        {
            Enable = setting.Walls.Enable,
            Min = setting.Walls.Min,
            Max = setting.Walls.Max
        },
        ScoreSaberStars = new RangeOption<float>
        {
            Enable = setting.Stars.Enable,
            Min = setting.Stars.Min,
            Max = setting.Stars.Max
        },
        ParityErrors = new RangeOption<int>
        {
            Enable = setting.ParityErrors.Enable,
            Min = setting.ParityErrors.Min,
            Max = setting.ParityErrors.Max
        },
        ParityWarns = new RangeOption<int>
        {
            Enable = setting.ParityWarns.Enable,
            Min = setting.ParityWarns.Min,
            Max = setting.ParityWarns.Max
        },
        ParityResets = new RangeOption<int>
        {
            Enable = setting.ParityResets.Enable,
            Min = setting.ParityResets.Min,
            Max = setting.ParityResets.Max
        },
        MaxScore = new RangeOption<int>
        {
            Enable = setting.MaxScore.Enable,
            Min = setting.MaxScore.Min,
            Max = setting.MaxScore.Max
        }
    };

    private static readonly char[] NewLines = ['\n', '\r'];

    private static SearchOptions ConvertSearchFilterOptions(LegacyPreset.SearchFilterSetting setting)
    {
        var rawLines = setting.SearchContent
            .Split(NewLines, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return new SearchOptions
        {
            Enable = setting.SearchEnabled,
            RegexPatterns = setting.RegexSearch ? rawLines.ToList() : [],
            AdvanceTerms = setting.RegexSearch ? [] : rawLines.Select(ParseSearchTerm).ToList(),
            SearchTitle = setting.SearchTitle,
            SearchSongName = setting.SearchSongName,
            SearchAuthor = setting.SearchAuthor,
            SearchMapper = setting.SearchMapper,
            SearchDescription = setting.SearchDescription
        };
    }

    /// <summary>
    ///     Parses a single legacy advanced-search line of the form <c>content|excl1,excl2</c>. The exclusion
    ///     segment is optional; the split on <c>|</c> is bounded to 2 parts so any stray pipe in the exclusion
    ///     segment is preserved as-is.
    /// </summary>
    private static AdvanceSearchTerm ParseSearchTerm(string raw)
    {
        var parts = raw.Split('|', 2);
        var exclusions = parts.Length == 2
            ? parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : [];
        return new AdvanceSearchTerm
        {
            Content = parts[0],
            Exclusions = exclusions
        };
    }
}
