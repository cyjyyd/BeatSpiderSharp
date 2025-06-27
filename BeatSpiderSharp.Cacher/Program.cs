using System.CommandLine;using System.CommandLine.Invocation;using System.CommandLine.Parsing;
using BeatSpiderSharp.Cacher;

var outputPath = new Option<string>("-o", "--output")
{
    Description = "输出路径",
    Required = true
};

var nThreads = new Option<int>("-t", "--threads")
{
    Required = false,
    Description = "并发数，默认为1",
    DefaultValueFactory = _ => 1
};

var useGzip = new Option<bool>("-z", "--gzip")
{
    Required = false,
    Description = "是否对输出文件使用 GZip 压缩，默认为否",
    DefaultValueFactory = _ => false
};

var rateLimit = new Option<int>("-r", "--rate-limit")
{
    Required = false,
    Description = "每次请求的最短时间，单位为毫秒，默认为0（不限速）",
    DefaultValueFactory = _ => 0
};

var indentedOutput = new Option<bool>("--indented")
{
    Required = false,
    Description = "是否对输出的 JSON 文件进行格式化，默认为否",
    DefaultValueFactory = _ => false
};

var rootCommand = new RootCommand("BeatSpider Cacher")
{
    outputPath,
    nThreads,
    useGzip,
    rateLimit,
    indentedOutput
};

rootCommand.TreatUnmatchedTokensAsErrors = true;

rootCommand.SetAction(async result =>
{
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

    using var crawler = new BeatSaverCrawler(progress)
    {
        UseGZip = result.GetRequiredValue(useGzip),
        MaxDegreeOfParallelism = result.GetRequiredValue(nThreads),
        MinRequestTime = result.GetRequiredValue(rateLimit),
        IndentedOutput = result.GetRequiredValue(indentedOutput)
    };

    var output = result.GetRequiredValue(outputPath);
    await crawler.CrawlAllMapsAsync(output);
    Console.WriteLine($"完整本地缓存已保存到 {output}");
});


return CommandLineParser.Parse(rootCommand, args, new CommandLineConfiguration(rootCommand)
{
    EnablePosixBundling = false
}).Invoke();



