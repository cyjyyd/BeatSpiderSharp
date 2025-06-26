using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text.Json;

namespace BeatSpiderSharp.Cacher;

public class BeatSaverCrawler : IDisposable
{
    private const string ApiUrl = "https://api.beatsaver.com/search/text/";
    private const int PageSize = 100;
    private readonly HttpClient _client;
    private readonly IProgress<ProgressReport> _progress;
    private readonly ConcurrentBag<string> _tempFiles = new();

    public BeatSaverCrawler(IProgress<ProgressReport> progress)
    {
        _progress = progress;
        _client = new HttpClient(new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 5
        });
    }

    public async Task CrawlAllMapsAsync(string outputPath)
    {
        var totalPages = await GetTotalPagesAsync();
        var options = new ParallelOptions { MaxDegreeOfParallelism = 2 };
        var writerLock = new object();
        using (var outputStream = new FileStream("localcache.saver", FileMode.Create))
        using (var writer = new Utf8JsonWriter(outputStream, new JsonWriterOptions
        {
            Indented = false
        }))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("docs");
            writer.WriteStartArray();
            await Parallel.ForEachAsync(Enumerable.Range(0, totalPages), options, async (page, ct) =>
            {
                using var inputStream = await ProcessPageAsync(page, totalPages);
                var doc = JsonDocument.Parse(inputStream);
                if (doc.RootElement.TryGetProperty("docs", out var docsArray))
                {
                    lock (writerLock)
                    {
                        foreach (var item in docsArray.EnumerateArray())
                        {
                            item.WriteTo(writer);
                        }
                    }
                }
            });
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
    }

    private async Task<int> GetTotalPagesAsync()
    {
        var response = await _client.GetStringAsync($"{ApiUrl}0?pageSize={PageSize}");
        var doc = JsonDocument.Parse(response);
        return (int)Math.Ceiling(doc.RootElement.GetProperty("info").GetProperty("total").GetInt32() / (double)PageSize);
    }

    private async Task<Stream> ProcessPageAsync(int page, int totalPages)
    {
        try
        {
            var response = await _client.GetStringAsync($"{ApiUrl}{page}?pageSize={PageSize}");
            _progress?.Report(new ProgressReport
            {
                CurrentPage = page + 1,
                TotalPages = totalPages
            });
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response));
            return memoryStream;
        }
        catch (Exception ex)
        {
            _progress?.Report(new ProgressReport { Error = ex });
            return Stream.Null;
        }
    }

    public void Dispose() => _client.Dispose();
}
