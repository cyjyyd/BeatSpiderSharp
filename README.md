# BeatSpiderSharp

.NET re-write of the [BeatSpider](https://github.com/WGzeyu/BeatSpider) tool for Beat Saber.

Currently there is only a CLI tool implemented. A GUI version is planned.

## Running the CLI

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

Run `dotnet run --project BeatSpiderSharp.CLI -c Release -- --help` to see how to run the CLI.

Latest cache data can be found in [BSC-ScrapeData](https://github.com/qe201020335/BSC-ScrapeData) ([direct download](https://raw.githubusercontent.com/qe201020335/BSC-ScrapeData/refs/heads/data/cache.json.gz)).

Presets from BeatSpider can be used directly with the flag `--legacy`.

