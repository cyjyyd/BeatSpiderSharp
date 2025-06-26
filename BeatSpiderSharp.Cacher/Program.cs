using BeatSpiderSharp.Cacher;

Console.ForegroundColor = ConsoleColor.Green;
var progress = new Progress<ProgressReport>(report =>
{
    if (report.Error != null)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"抓取页面 {report.CurrentPage} 时发生错误: {report.Error.Message}");
        Console.ForegroundColor = ConsoleColor.Green;
    }
    else
    {
        Console.WriteLine($"已获取 {report.CurrentPage}/{report.TotalPages} 页 ({Math.Round((double)report.CurrentPage / report.TotalPages * 100, 2)}%)");
    }
});

using var crawler = new BeatSaverCrawler(progress);
await crawler.CrawlAllMapsAsync("localcache.saver");
Console.WriteLine("完整本地缓存已保存到 localcache.saver");
