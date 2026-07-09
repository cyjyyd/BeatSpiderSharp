namespace BeatSpiderSharp.CLI;

public record BeatSpiderOptions
{
    public required string InputPreset { get; init; }

    public required string SongCachePath { get; init; }

    public string? LocalZipsPath { get; init; }

    public bool GZipCacheData { get; init; }

    public string? PlaylistDirectory { get; init; }

    public string? OutputSongPath { get; init; }

    public string? PresetAuthor { get; init; }

    public bool DisablePlaylistOutput { get; init; }

    public bool DisableSongDownload { get; init; }

    public bool SkipExistingSongs { get; init; }

    public bool? SaveSongZips { get; init; }

    public bool? UseLocalZips { get; init; }

    public bool InputIsLegacy { get; init; }

    public string? SaveConvertedPresetPath { get; init; }

    public bool ConvertPresetAndExit { get; init; }

    public bool Verbose { get; init; }
}
