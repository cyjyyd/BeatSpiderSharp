using System.CommandLine;

namespace BeatSpiderSharp.CLI.CLIRunners;

internal class BeatSpiderRunner : RunnerBase
{
    private readonly Option<string> _inputPreset = new("--input-preset", "-i")
    {
        Required = true,
        Description = "Input preset file path"
    };

    private readonly Option<string> _songCachePath = new("--song-cache-data", "-s")
    {
        Required = true,
        Description = "Song cache data file path"
    };

    private readonly Option<string> _outputPlaylist = new("--output-playlist", "-o")
    {
        Description = "Output playlist file path"
    };

    private readonly Option<string> _outputSongPath = new("--output-song-path", "-O")
    {
        Description = "Output song path"
    };

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

    private readonly Option<bool> _inputIsLegacy = new("--legacy")
    {
        Description = "Input preset is in legacy format",
        DefaultValueFactory = _ => false
    };

    private readonly Option<string> _saveConvertedPresetPath = new("--save-preset")
    {
        Description = "Save converted preset to file, only works with legacy input preset"
    };

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
        _outputPlaylist,
        _outputSongPath,
        _presetAuthor,
        _disablePlaylistOutput,
        _disableSongDownload,
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
            OutputPlaylist = parseResult.GetValue(_outputPlaylist),
            OutputSongPath = parseResult.GetValue(_outputSongPath),
            PresetAuthor = parseResult.GetValue(_presetAuthor),
            DisablePlaylistOutput = parseResult.GetRequiredValue(_disablePlaylistOutput),
            DisableSongDownload = parseResult.GetRequiredValue(_disableSongDownload),
            InputIsLegacy = parseResult.GetRequiredValue(_inputIsLegacy),
            SaveConvertedPresetPath = parseResult.GetValue(_saveConvertedPresetPath),
            ConvertPresetAndExit = parseResult.GetRequiredValue(_convertPresetAndExit),
            Verbose = parseResult.GetRequiredValue(_verbose)
        };

        using var beatSpider = new BeatSpiderCLI(options.Verbose);

        return await beatSpider.Run(options);
    }
}
