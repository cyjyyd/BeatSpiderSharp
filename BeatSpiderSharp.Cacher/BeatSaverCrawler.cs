using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Text.Json;

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

    public async Task CrawlAllMapsAsync(string outputPath)
    {
        var totalPages = await GetTotalPagesAsync();
        var options = new ParallelOptions { MaxDegreeOfParallelism = 2 };
        var writerLock = new object();

        await using var outputStream = new FileStream(outputPath, FileMode.Create);

        await using var writer = new Utf8JsonWriter(outputStream, new JsonWriterOptions
        {
            Indented = false
        });
        writer.WriteStartObject();
        writer.WritePropertyName("docs");
        writer.WriteStartArray();
        await Parallel.ForEachAsync(Enumerable.Range(0, totalPages), options, async (page, ct) =>
        {
            await using var inputStream = await ProcessPageAsync(page, totalPages);
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
            progress?.Report(new ProgressReport
            {
                CurrentPage = page + 1,
                TotalPages = totalPages
            });
            var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(response));
            return memoryStream;
        }
        catch (Exception ex)
        {
            progress?.Report(new ProgressReport { Error = ex });
            return Stream.Null;
        }
    }
}
