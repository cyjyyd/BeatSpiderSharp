using System.Text.RegularExpressions;
using BeatSpiderSharp.Extensions;
using BeatSpiderSharp.Models.Enums;
using BeatSpiderSharp.Models.Preset;
using BeatSpiderSharp.Models.Preset.FilterOptions;
using Newtonsoft.Json;
using Serilog;

namespace BeatSpiderSharp.Legacy;

public static class LegacyPresetLoader
{
    //beatsaver.com/profile/58338
    private static readonly Regex MapperUrlRegex = new(@"beatsaver.com/profile/(\d+)", RegexOptions.Compiled);

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
        var options = ConvertFilterOptions(legacyPreset.SongFilter);
        var output = new OutputConfig
        {
            LimitSongs = legacyPreset.Limits.Count.Enable,
            MaxSongs = legacyPreset.Limits.Count.Content,
            SavePlaylist = legacyPreset.Output.Playlist.Enable,
            PostProcessPlaylist = true,
            PlaylistPath = legacyPreset.Output.Playlist.Path,
            DownloadSongs = legacyPreset.Output.Songs.Enable,
            DownloadPath = legacyPreset.Output.Songs.Path,
            SkipExisting = true,
            ExistingSongPaths = legacyPreset.LocalSong.LocalSongSkipPaths.ToList(),
            CopyLocalSongs = true,
            LocalSongPaths = legacyPreset.LocalSong.LocalSongPaths.ToList()
        };
        var input = new InputConfig
        {
            Source = SongInputSource.BeatSaver,
            Playlists = string.IsNullOrWhiteSpace(legacyPreset.PlaylistInput.Path)
                ? []
                : [legacyPreset.PlaylistInput.Path],
            ManualInput = legacyPreset.ManualSongInput.Songs.ToList()
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
                MergeMapperSetting(options, legacyPreset.Mapper);
                input.Source = SongInputSource.BeatSaver;
                break;
            case LegacyPreset.DataSource.ScoreSaber:
                MergeScoreSaverSetting(options, legacyPreset.ScoreSaber);
                input.Source = SongInputSource.BeatSaver;
                break;
            case LegacyPreset.DataSource.BeatSaver:
                MergeBeatSaverSetting(options, legacyPreset.BeatSaver);
                input.Source = SongInputSource.BeatSaver;
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
            FilterOptions = [new FilterConfig { DetailFilter = options }]
        };

#if DEBUG
        Log.Debug("Legacy preset: {@LegacyPreset}", legacyPreset);
        Log.Debug("Converted preset: {@NewPreset}", preset);
#endif
        return preset;
    }
    
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
        if (preset.SearchFilter.SearchEnabled)
        {
            //TODO
            throw new LegacyConversionException("Search filter is not implemented");
        }
        
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

    private static void MergeBeatSaverSetting(DetailOptions options, LegacyPreset.BeatSaverSetting setting)
    {
        Log.Debug("Merging BeatSaver source settings into filter options");
        if (!string.IsNullOrWhiteSpace(setting.SearchKeyword) || setting.StartPage.HasValue)
        {
            //TODO add search keyword to search filter settings once search filter is supported
            throw new LegacyConversionException("BeatSaver search keyword and starting page are not supported",
                "BeatSaver Search");
        }

        if (setting.Sort != LegacyPreset.BeatSaverSetting.SortType.Latest)
        {
            throw new LegacyConversionException(
                $"Only {nameof(LegacyPreset.BeatSaverSetting.SortType.Latest)} BeatSaver search sort type is supported",
                "BeatSaver Search");
        }

        // following BeatSaver API's handling
        var (autoMapperEnable, autoMapperFilter) = setting.AutoMapper switch
        {
            { Enable: false } => (true, false), // no auto mapped 
            { AutoMapper: true } => (false, false), // all maps
            { AutoMapper: false } => (true, false) // only auto mapped
        };

        if (options.AutoMapper.Enable && autoMapperEnable && options.AutoMapper.Filter != autoMapperFilter)
        {
            throw new LegacyConversionException(
                "Conflicting auto mapper settings from BeatSaver search setting and song filter setting",
                "AutoMapper");
        }

        if (autoMapperEnable)
        {
            options.AutoMapper.Enable = true;
            options.AutoMapper.Filter = autoMapperFilter;
        }

        if (setting.RankedSong.Enable && setting.RankedSong.IsRanked)
        {
            if (options.ScoreSaberRanking.Enable)
            {
                Log.Warning("Overwriting ScoreSaber option to {Status}", RankingStatus.Ranked);
            }

            options.ScoreSaberRanking.Enable = true;
            options.ScoreSaberRanking.Filter = new HashSet<RankingStatus> { RankingStatus.Ranked };
        }

        options.FullSpread.Enable = setting.Difficulty.Enable;
        options.FullSpread.Filter = setting.Difficulty.IsFullSpread;

        if (setting.Bpm.Enable)
        {
            Log.Debug("Merging BeatSaver BPM setting into filter options");
            CombineRange(options.Bpm, setting.Bpm, "Bpm");
        }

        if (setting.Nps.Enable)
        {
            Log.Information("Merging BeatSaver NPS setting into filter options");
            CombineRange(options.Nps, setting.Nps, "Nps");
        }

        if (setting.Duration.Enable)
        {
            Log.Information("Merging BeatSaver duration setting into filter options");
            CombineRange(options.Duration, setting.Duration, "Duration");
        }

        if (setting.UploadTime.Enable)
        {
            Log.Information("Merging BeatSaver upload time setting into filter options");
            CombineRange(options.UploadTime, setting.UploadTime, "UploadTime");
        }

        if (setting.Rating.Enable)
        {
            Log.Information("Merging BeatSaver rating setting into filter options");
            CombineRange(options.Rating, setting.Rating, "Rating");
        }

        if (setting.RequireMods.Enable)
        {
            options.RequireMods.Enable = true;
            options.RequireMods.Filter = options.RequireMods.Filter
                .Concat(setting.RequireMods.RequireMods.ToMMods()).ToHashSet();
        }

        if (setting.ExcludeMods.Enable)
        {
            options.ExcludeMods.Enable = true;
            options.ExcludeMods.Filter = options.ExcludeMods.Filter
                .Concat(setting.ExcludeMods.ExcludeMods.ToMMods()).ToHashSet();
        }
    }

    private static void MergeScoreSaverSetting(DetailOptions options, LegacyPreset.ScoreSaberSetting setting)
    {
        Log.Debug("Merging ScoreSaber source setting into filter options");
        options.ScoreSaberRanking.Enable = true;
        options.ScoreSaberRanking.Filter = new HashSet<RankingStatus> { RankingStatus.Ranked };
        CombineRange(options.ScoreSaberStars, setting.StarDifficulty, "ScoreSaber stars");
    }

    private static void MergeMapperSetting(DetailOptions options, LegacyPreset.MapperSetting setting)
    {
        var url = setting.MapperAddress;
        Log.Debug("Extracting uploader id from mapper url: {Url}", url);
        var match = MapperUrlRegex.Match(url);
        if (match is { Success: true, Groups.Count: 2 } && int.TryParse(match.Groups[1].Value, out var id))
        {
            Log.Information("Found uploader id from mapper url: {Url}", url);
            if (options.UploaderId.Enable && !options.UploaderId.Filter.Contains(id))
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

    private static DetailOptions ConvertFilterOptions(LegacyPreset.SongFilterSetting setting)
    {
        return new DetailOptions
        {
            UploaderId =
            {
                Enable = setting.UploaderIds.Enable,
                Filter = setting.UploaderIds.Content.ToHashSet()
            },
            UploaderName =
            {
                Enable = setting.UploaderNames.Enable,
                Filter = setting.UploaderNames.Content.ToHashSet()
            },
            UploadTime = new()
            {
                Enable = setting.UploadTime.Enable,
                Min = setting.UploadTime.Min,
                Max = setting.UploadTime.Max
            },
            IncludeTags = new()
            {
                Enable = setting.Tags.Include.Enable,
                Filter = setting.Tags.Include.Content.ToHashSet(),
                IsOr = !setting.Tags.Include.And
            },
            ExcludeTags =
            {
                Enable = setting.Tags.Exclude.Enable,
                Filter = setting.Tags.Exclude.Content.ToHashSet(),
                IsOr = !setting.Tags.Exclude.And
            },
            UpVotes = new()
            {
                Enable = setting.UpVotes.Enable,
                Min = setting.UpVotes.Min,
                Max = setting.UpVotes.Max
            },
            UpVotePercentage = new()
            {
                Enable = setting.UpVotePercentage.Enable,
                Min = setting.UpVotePercentage.Min,
                Max = setting.UpVotePercentage.Max
            },
            DownVotes = new()
            {
                Enable = setting.DownVotes.Enable,
                Min = setting.DownVotes.Min,
                Max = setting.DownVotes.Max
            },
            DownVotePercentage = new()
            {
                Enable = setting.DownVotePercentage.Enable,
                Min = setting.DownVotePercentage.Min,
                Max = setting.DownVotePercentage.Max
            },
            Rating = new()
            {
                Enable = setting.Rating.Enable,
                Min = setting.Rating.Min,
                Max = setting.Rating.Max
            },
            IncludeCharacteristics = new()
            {
                Enable = setting.IncludeCharacteristics.Enable,
                Filter = setting.IncludeCharacteristics.Characteristics.Select(EnumConversions.ToMCharacteristic)
                    .ToHashSet(),
                IsOr = true
            },
            IncludeDifficulties = new()
            {
                Enable = setting.IncludeDifficulties.Enable,
                Filter = setting.IncludeDifficulties.Difficulties.Select(EnumConversions.ToMDifficulty).ToHashSet(),
                IsOr = !setting.IncludeDifficulties.And
            },
            RequireMods = new()
            {
                Enable = setting.RequireMods.Enable,
                Filter = setting.RequireMods.Mods.ToMMods(),
                IsOr = true
            },
            ExcludeMods =
            {
                Enable = setting.ExcludeMods.Enable,
                Filter = setting.ExcludeMods.Mods.ToMMods()
            },
            Downloads =
            {
                Enable = setting.DownloadCount.Enable,
                Min = setting.DownloadCount.Min,
                Max = setting.DownloadCount.Max
            },
            Plays =
            {
                Enable = setting.PlayCount.Enable,
                Min = setting.PlayCount.Min,
                Max = setting.PlayCount.Max
            },
            AutoMapper =
            {
                Enable = setting.AutoMapper.Enable,
                Filter = setting.AutoMapper.AutoMapper
            },
            Bpm = new()
            {
                Enable = setting.Bpm.Enable,
                Min = setting.Bpm.Min,
                Max = setting.Bpm.Max
            },
            Duration = new()
            {
                Enable = setting.Duration.Enable,
                Min = setting.Duration.Min,
                Max = setting.Duration.Max
            },
            Seconds =
            {
                Enable = setting.MapSeconds.Enable,
                Min = setting.MapSeconds.Min,
                Max = setting.MapSeconds.Max
            },
            Beats =
            {
                Enable = setting.MapLength.Enable,
                Min = setting.MapLength.Min,
                Max = setting.MapLength.Max
            },
            Njs = new()
            {
                Enable = setting.Njs.Enable,
                Min = setting.Njs.Min,
                Max = setting.Njs.Max
            },
            Offset =
            {
                Enable = setting.Offset.Enable,
                Min = setting.Offset.Min,
                Max = setting.Offset.Max
            },
            Nps = new()
            {
                Enable = setting.Nps.Enable,
                Min = setting.Nps.Min,
                Max = setting.Nps.Max
            },
            Notes = new()
            {
                Enable = setting.Notes.Enable,
                Min = setting.Notes.Min,
                Max = setting.Notes.Max
            },
            Bombs = new()
            {
                Enable = setting.Bombs.Enable,
                Min = setting.Bombs.Min,
                Max = setting.Bombs.Max
            },
            Events =
            {
                Enable = setting.Events.Enable,
                Min = setting.Events.Min,
                Max = setting.Events.Max
            },
            Walls = new()
            {
                Enable = setting.Walls.Enable,
                Min = setting.Walls.Min,
                Max = setting.Walls.Max
            },
            ScoreSaberRanking =
            {
                Enable = setting.RankedSong.Enable,
                Filter = setting.RankedSong.IsRanked
                    ? new HashSet<RankingStatus> { RankingStatus.Ranked }
                    : new HashSet<RankingStatus> { RankingStatus.Unranked, RankingStatus.Qualified }
            },
            ScoreSaberStars = new()
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
            SageScore = new RangeOption<int>
            {
                Enable = setting.SageScore.Enable,
                Min = setting.SageScore.Min,
                Max = setting.SageScore.Max
            },
            MaxScore = new RangeOption<int>
            {
                Enable = setting.MaxScore.Enable,
                Min = setting.MaxScore.Min,
                Max = setting.MaxScore.Max
            },
            Chinese = new()
            {
                Enable = setting.FilterChinese.Enable
            }
        };
    }
}
