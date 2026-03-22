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

    public bool Verbose { get; init; }

    public bool ExitOnError { get; init; }

    void IDisposable.Dispose()
    {
        _client.Dispose();
        GC.SuppressFinalize(this);
    }

    public async Task CrawlAllMapsAsync(string outputPath, CancellationToken cToken)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("输出路径不能为空", nameof(outputPath));
        }

        if (File.Exists(outputPath) && Verbose)
        {
            Console.WriteLine("将覆盖已存在输出文件");
        }

        var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await CrawlAllMapsAsync(fileStream, false, cToken);
    }

    public async Task CrawlAllMapsAsync(Stream outputStream, bool leaveOpen, CancellationToken cToken)
    {
        if (!outputStream.CanWrite)
        {
            throw new ArgumentException("输出流不可写入", nameof(outputStream));
        }

        try
        {
            if (UseGZip)
            {
                await using var gZipStream = new GZipStream(outputStream, CompressionLevel.SmallestSize, true);
                await CrawlAllMapsAsync(gZipStream, cToken);
            }
            else
            {
                await CrawlAllMapsAsync(outputStream, cToken);
            }
        }
        finally
        {
            if (!leaveOpen) await outputStream.DisposeAsync();
        }
    }

    private async Task CrawlAllMapsAsync(Stream outputStream, CancellationToken cToken)
    {
        if (ConcurrentRequests <= 0)
        {
            throw new InvalidOperationException("并发数必须大于0");
        }

        if (MinRequestTime < 0)
        {
            throw new InvalidOperationException("最小请求时间不能小于0");
        }

        var totalPages = await GetTotalPagesAsync();

        await using var textWriter = new StreamWriter(outputStream);
        await using var writer = new JsonTextWriter(textWriter);
        writer.Formatting = IndentedOutput ? Formatting.Indented : Formatting.None;

        await writer.WriteStartObjectAsync(cToken);
        await writer.WritePropertyNameAsync("docs", cToken);
        await writer.WriteStartArrayAsync(cToken);

        await writer.FlushAsync(cToken);
        GC.Collect();

        var stopwatch = new Stopwatch();
        var tasks = new List<Task<JArray?>>(ConcurrentRequests);
        for (var basePage = 0; basePage < totalPages; basePage += ConcurrentRequests)
        {
            stopwatch.Reset();
            stopwatch.Start();
            tasks.Clear();
            var endPage = 0;
            try
            {
                for (var offset = 0; offset < ConcurrentRequests && basePage + offset < totalPages; offset++)
                {
                    var page = basePage + offset;
                    tasks.Add(Task.Run(() => CrawlPageAsync(page, cToken), cToken));
                    endPage = page;
                }

                await Task.WhenAll(tasks);
                foreach (var task in tasks)
                {
                    var docs = await task;
                    if (docs is null) continue;

                    foreach (var item in docs)
                    {
                        await item.WriteToAsync(writer, cToken);
                    }
                }

                await writer.FlushAsync(cToken);
                progress?.Report(new ProgressReport
                {
                    CurrentPage = endPage + 1,
                    TotalPages = totalPages
                });
                GC.Collect();
            }
            catch (OperationCanceledException)
            {
                if (Verbose) Console.WriteLine("操作被取消");
                throw;
            }
            catch (Exception ex)
            {
                progress?.Report(new ProgressReport
                {
                    CurrentPage = endPage + 1,
                    TotalPages = totalPages,
                    Error = ex
                });
                if (ExitOnError) throw;
            }
            finally
            {
                stopwatch.Stop();
            }

            if (cToken.IsCancellationRequested) return;
            var elapsedTime = (int)stopwatch.Elapsed.TotalMilliseconds;
            if (MinRequestTime > 0 && elapsedTime < MinRequestTime)
            {
                var delay = MinRequestTime - elapsedTime;
                if (Verbose)
                {
                    Console.WriteLine($"第 {basePage + 1}-{endPage + 1} 页耗时 {elapsedTime}ms, 添加额外延迟 {delay}ms");
                }

                await Task.Delay(delay, cToken);
            }
            else
            {
                if (Verbose) Console.WriteLine($"第 {basePage + 1}-{endPage + 1} 页耗时 {elapsedTime}ms");
            }
        }

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

    private async Task<JArray?> CrawlPageAsync(int page, CancellationToken ct)
    {
        await using var resStream = await _client.GetStreamAsync($"{ApiUrl}{page}?pageSize={PageSize}", ct);
        using var streamReader = new StreamReader(resStream, Encoding.UTF8);
        await using var jsonReader = new JsonTextReader(streamReader);

        var res = await JObject.LoadAsync(jsonReader, ct);
        var docs = res["docs"];

        if (docs is null || !docs.HasValues)
        {
            var ex = new Exception($"第 {page + 1} 页没有数据");
            progress?.Report(new ProgressReport { Error = ex });
            return ExitOnError ? throw ex : null;
        }

        if (docs.Type != JTokenType.Array || docs is not JArray array)
        {
            var ex = new Exception($"第 {page + 1} 页数据错误， 'docs' 不是一个数组");
            progress?.Report(new ProgressReport { Error = ex });
            return ExitOnError ? throw ex : null;
        }

        return array;
    }
}
