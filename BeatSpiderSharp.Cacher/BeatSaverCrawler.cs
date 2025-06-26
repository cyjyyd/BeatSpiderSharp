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

    public async Task CrawlAllMapsAsync(string outputPath)
    {
        var totalPages = await GetTotalPagesAsync();
        var options = new ParallelOptions { MaxDegreeOfParallelism = 2 };
        var writerLock = new object();

        await using var outputStream = new FileStream(outputPath, FileMode.Create);
        await using var textWriter = new StreamWriter(outputStream);
        await using var writer = new JsonTextWriter(textWriter);
        writer.Formatting = Formatting.Indented;

        await writer.WriteStartObjectAsync();
        await writer.WritePropertyNameAsync("docs");
        await writer.WriteStartArrayAsync();
        await Parallel.ForEachAsync(Enumerable.Range(0, totalPages), options, async (page, ct) =>
        {
            await using var inputStream = await ProcessPageAsync(page, totalPages);
            using var streamReader = new StreamReader(inputStream);
            await using var jsonReader = new JsonTextReader(streamReader);
            var doc = await JObject.LoadAsync(jsonReader, ct);
            var docs = doc["docs"];
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
        });
        await writer.WriteEndArrayAsync();
        await writer.WriteEndObjectAsync();
    }

    private async Task<int> GetTotalPagesAsync()
    {
        var response = await _client.GetStringAsync($"{ApiUrl}0?pageSize={PageSize}");
        var doc = JObject.Parse(response);
        return (int)Math.Ceiling(doc["info"]!["total"]!.ToObject<double>() / PageSize);
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
            progress?.Report(new ProgressReport
            {
                CurrentPage = page + 1,
                TotalPages = totalPages,
                Error = ex
            });
            return Stream.Null;
        }
    }
}
