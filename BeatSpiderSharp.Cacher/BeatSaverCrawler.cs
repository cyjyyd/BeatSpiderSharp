using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BeatSpiderSharp.Cacher;

public class BeatSaverCrawler(IProgress<ProgressReport>? progress) : IDisposable
{
    private const string ApiUrl = "https://api.beatsaver.com/search/text/";
    private const int PageSize = 100;

    private readonly HttpClient _client = new(new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        MaxConnectionsPerServer = 5
    });

    ~BeatSaverCrawler()
    {
        _client.Dispose();
    }

    void IDisposable.Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task CrawlAllMapsAsync(string outputPath, bool gzip, bool indented = false)
    {
        var totalPages = await GetTotalPagesAsync();
        var options = new ParallelOptions { MaxDegreeOfParallelism = 2 };
        var writerLock = new object();

        await using Stream outputStream = gzip
            ? new GZipStream(new FileStream(outputPath, FileMode.Create), CompressionLevel.SmallestSize, false)
            : new FileStream(outputPath, FileMode.Create);
        await using var textWriter = new StreamWriter(outputStream);
        await using var writer = new JsonTextWriter(textWriter);
        writer.Formatting = indented ? Formatting.Indented : Formatting.None;

        await writer.WriteStartObjectAsync();
        await writer.WritePropertyNameAsync("docs");
        await writer.WriteStartArrayAsync();
        await Parallel.ForEachAsync(Enumerable.Range(0, totalPages), options, async (page, ct) =>
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await using var resStream = await _client.GetStreamAsync($"{ApiUrl}{page}?pageSize={PageSize}", ct);
                using var streamReader = new StreamReader(resStream, Encoding.UTF8);
                await using var jsonReader = new JsonTextReader(streamReader);

                var res = await JObject.LoadAsync(jsonReader, ct);
                var docs = res["docs"];

                if (docs is null || !docs.HasValues)
                {
                    return; // Skip empty pages
                }

                if (docs.Type != JTokenType.Array)
                {
                    progress?.Report(new ProgressReport { Error = new Exception("'docs' is not an array.") });
                    return;
                }

                lock (writerLock)
                {
                    foreach (var item in docs)
                    {
                        item.WriteTo(writer);
                    }
                }

                progress?.Report(new ProgressReport
                {
                    CurrentPage = page + 1,
                    TotalPages = totalPages
                });
            }
            catch (Exception ex)
            {
                progress?.Report(new ProgressReport
                {
                    CurrentPage = page + 1,
                    TotalPages = totalPages,
                    Error = ex
                });
            }
            finally
            {
                stopwatch.Stop();
                // var elapsedTime = stopwatch.Elapsed.TotalMilliseconds;
                //
                // // BeatSaver has a rate limit of 10 requests per second. (LIES!!! MORE LIKE 4!)
                // var delay = 250 - elapsedTime;
                //
                // if (delay > 0)
                // {
                //     Console.WriteLine($"{elapsedTime}ms, delaying {delay}ms");
                //     await Task.Delay((int)delay, ct);
                // }
                // else
                // {
                //     Console.WriteLine($"{elapsedTime}ms");
                // }
            }
        });
        await writer.WriteEndArrayAsync();
        await writer.WritePropertyNameAsync("date");
        await writer.WriteValueAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await writer.WriteEndObjectAsync();
    }

    private async Task<int> GetTotalPagesAsync()
    {
        var response = await _client.GetStringAsync($"{ApiUrl}0?pageSize={PageSize}");
        var doc = JObject.Parse(response);
        return (int)Math.Ceiling(doc["info"]!["total"]!.ToObject<double>() / PageSize);
    }
}
