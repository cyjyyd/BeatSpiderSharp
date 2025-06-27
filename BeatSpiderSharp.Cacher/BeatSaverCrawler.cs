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

    public bool UseGZip { get; init; }

    public int ConcurrentRequests { get; init; } = 1;

    public bool IndentedOutput { get; init; }

    public int MinRequestTime { get; init; }


    ~BeatSaverCrawler()
    {
        _client.Dispose();
    }

    void IDisposable.Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task CrawlAllMapsAsync(string outputPath, CancellationToken cToken)
    {
        if (ConcurrentRequests <= 0)
        {
            throw new Exception("并发数必须大于0");
        }

        var totalPages = await GetTotalPagesAsync();
        var options = new ParallelOptions { MaxDegreeOfParallelism = ConcurrentRequests, CancellationToken = cToken };
        var writerLock = new object();

        await using Stream outputStream = UseGZip
            ? new GZipStream(new FileStream(outputPath, FileMode.Create), CompressionLevel.SmallestSize, false)
            : new FileStream(outputPath, FileMode.Create);
        await using var textWriter = new StreamWriter(outputStream);
        await using var writer = new JsonTextWriter(textWriter);
        writer.Formatting = IndentedOutput ? Formatting.Indented : Formatting.None;

        await writer.WriteStartObjectAsync(cToken);
        await writer.WritePropertyNameAsync("docs", cToken);
        await writer.WriteStartArrayAsync(cToken);
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
            catch (OperationCanceledException)
            {
                throw;
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
                var elapsedTime = stopwatch.Elapsed.TotalMilliseconds;

                if (MinRequestTime > 0 && elapsedTime < MinRequestTime)
                {
                    var delay = (int)(MinRequestTime - elapsedTime);
                    Console.WriteLine($"{elapsedTime}ms, delaying {delay}ms");
                    await Task.Delay(delay, ct);
                }
                else
                {
                    Console.WriteLine($"{elapsedTime}ms");
                }
            }
        });
        await writer.WriteEndArrayAsync(cToken);
        await writer.WritePropertyNameAsync("date", cToken);
        await writer.WriteValueAsync(DateTimeOffset.UtcNow.ToUnixTimeSeconds(), cToken);
        await writer.WriteEndObjectAsync(cToken);
    }

    private async Task<int> GetTotalPagesAsync()
    {
        var response = await _client.GetStringAsync($"{ApiUrl}0?pageSize={PageSize}");
        var doc = JObject.Parse(response);
        return (int)Math.Ceiling(doc["info"]!["total"]!.ToObject<double>() / PageSize);
    }
}
