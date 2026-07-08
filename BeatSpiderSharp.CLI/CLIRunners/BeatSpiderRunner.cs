using System.CommandLine;

namespace BeatSpiderSharp.CLI.CLIRunners;

internal class BeatSpiderRunner : RunnerBase
{
    private readonly Option<string> _inputPreset = new Option<string>("--input-preset", "-i")
    {
        Required = true,
        Description = "Input preset file path"
    }.AcceptLegalFilePathsOnly();

    private readonly Option<string> _songCachePath = new Option<string>("--song-cache-data", "-s")
    {
        Required = true,
        Description = "Song cache data file path"
    }.AcceptLegalFilePathsOnly();

    private readonly Option<string> _localZipsPath = new Option<string>("--local-zips-path", "-S")
    {
        Description = "Local song zips path"
    }.AcceptLegalFilePathsOnly();

    private readonly Option<bool> _gZipCacheData = new("--gzip-cache-data", "-z")
    {
        Description = "Song cache data is GZip format",
        DefaultValueFactory = _ => false
    };

    private readonly Option<string> _outputPlaylist = new Option<string>("--output-playlist", "-o")
    {
        Description = "Output playlist file path"
    }.AcceptLegalFilePathsOnly();

    private readonly Option<string> _outputSongPath = new Option<string>("--output-song-path", "-O")
    {
        Description = "Output song path"
    }.AcceptLegalFilePathsOnly();

    private readonly Option<string> _presetAuthor = new("--preset-author")
    {
        Description = "Preset author"
    };

    private readonly Option<bool> _disablePlaylistOutput = new("--disable-playlist-output", "-d")
    {
        Description = "Disable playlist output",
        DefaultValueFactory = _ => false
    };

    private readonly Option<bool> _disableSongDownload = new("--disable-song-download", "-D")
    {
        Description = "Disable song download",
        DefaultValueFactory = _ => false
    };

    private readonly Option<bool?> _saveSongZips = new("--save-song-zips", "-l")
    {
        Description = "Save song zips",
        DefaultValueFactory = _ => null
    };

    private readonly Option<bool?> _useLocalZips = new("--use-local-zips", "-L")
    {
        Description = "Use local song zips",
        DefaultValueFactory = _ => null
    };

    private readonly Option<bool> _inputIsLegacy = new("--legacy")
    {
        Description = "Input preset is in legacy format",
        DefaultValueFactory = _ => false
    };

    private readonly Option<string> _saveConvertedPresetPath = new Option<string>("--save-preset")
    {
        Description = "Save converted preset to file, only works with legacy input preset"
    }.AcceptLegalFilePathsOnly();

    private readonly Option<bool> _convertPresetAndExit = new("--convert-only")
    {
        Description = "Convert preset and exit",
        DefaultValueFactory = _ => false
    };

    private readonly Option<bool> _verbose = new("--verbose", "-v")
    {
        Description = "Verbose filter logging",
        DefaultValueFactory = _ => false
    };

    protected override Option[] Options =>
    [
        _inputPreset,
        _songCachePath,
        _localZipsPath,
        _gZipCacheData,
        _outputPlaylist,
        _outputSongPath,
        _presetAuthor,
        _disablePlaylistOutput,
        _disableSongDownload,
        _saveSongZips,
        _useLocalZips,
        _inputIsLegacy,
        _saveConvertedPresetPath,
        _convertPresetAndExit,
        _verbose
    ];

    protected override string Description => "BeatSpider CLI";

    protected override async Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken)
    {
        var options = new BeatSpiderOptions
        {
            InputPreset = parseResult.GetRequiredValue(_inputPreset),
            SongCachePath = parseResult.GetRequiredValue(_songCachePath),
            LocalZipsPath = parseResult.GetValue(_localZipsPath),
            GZipCacheData = parseResult.GetRequiredValue(_gZipCacheData),
            PlaylistDirectory = parseResult.GetValue(_outputPlaylist),
            OutputSongPath = parseResult.GetValue(_outputSongPath),
            PresetAuthor = parseResult.GetValue(_presetAuthor),
            DisablePlaylistOutput = parseResult.GetRequiredValue(_disablePlaylistOutput),
            DisableSongDownload = parseResult.GetRequiredValue(_disableSongDownload),
            SaveSongZips = parseResult.GetValue(_saveSongZips),
            UseLocalZips = parseResult.GetValue(_useLocalZips),
            InputIsLegacy = parseResult.GetRequiredValue(_inputIsLegacy),
            SaveConvertedPresetPath = parseResult.GetValue(_saveConvertedPresetPath),
            ConvertPresetAndExit = parseResult.GetRequiredValue(_convertPresetAndExit),
            Verbose = parseResult.GetRequiredValue(_verbose)
        };

        using var beatSpider = new BeatSpiderCLI(options.Verbose);

        return await beatSpider.RunAsync(options, cancellationToken);
    }
}
