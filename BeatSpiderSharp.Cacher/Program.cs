using System.CommandLine;
using System.CommandLine.Parsing;
using BeatSpiderSharp.Cacher;
using BeatSpiderSharp.Shared;

var defaultColor = Console.ForegroundColor;
var cTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (o, e) =>
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("取消操作...");
    Console.ForegroundColor = defaultColor;
    e.Cancel = true; // Prevent the process from terminating immediately
    cTokenSource.Cancel();
};

AppDomain.CurrentDomain.UnhandledException += (o, e) =>
{
    if (e.ExceptionObject is OperationCanceledException)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("操作已取消。");
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("未处理的异常: ");
        if (e.ExceptionObject is Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
        else
        {
            Console.WriteLine(e.ExceptionObject);
        }
    }

    Console.ForegroundColor = e.IsTerminating ? defaultColor : ConsoleColor.Green;
};

AppDomain.CurrentDomain.ProcessExit += (o, e) =>
{
    // Restore console color
    Console.ForegroundColor = defaultColor;
};

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
nThreads.Validators.Add(result =>
{
    if (result.GetValue(nThreads) <= 0)
    {
        result.AddError("并发数必须大于或等于1");
    }
});

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
rateLimit.Validators.Add(result =>
{
    if (result.GetValue(rateLimit) < 0)
    {
        result.AddError("每次请求的最短时间必须不能小于0");
    }
});

var exitOnError = new Option<bool>("-e", "--exit-on-error")
{
    Required = false,
    Description = "发生错误时是否停止操作，默认为否",
    DefaultValueFactory = _ => false
};

var indentedOutput = new Option<bool>("--indented")
{
    Required = false,
    Description = "是否对输出的 JSON 文件进行格式化，默认为否",
    DefaultValueFactory = _ => false
};

var verbose = new Option<bool>("-v", "--verbose")
{
    Required = false,
    Description = "是否启用详细输出，默认为否",
    DefaultValueFactory = _ => false
};

var rootCommand = new RootCommand("BeatSpider Cacher")
{
    outputPath,
    nThreads,
    useGzip,
    rateLimit,
    exitOnError,
    indentedOutput,
    verbose
};

rootCommand.TreatUnmatchedTokensAsErrors = true;

rootCommand.SetAction(async (result, cToken) =>
{
    Console.WriteLine($"BeatSpider.Cacher v{Constants.Version.ToFullString()}");
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
            Console.WriteLine($"已获取 {report.CurrentPage}/{report.TotalPages} 页 ({report.CurrentPage / (double) report.TotalPages:P2})");
        }
    });

    using var crawler = new BeatSaverCrawler(progress)
    {
        UseGZip = result.GetRequiredValue(useGzip),
        ConcurrentRequests = result.GetRequiredValue(nThreads),
        MinRequestTime = result.GetRequiredValue(rateLimit),
        IndentedOutput = result.GetRequiredValue(indentedOutput),
        Verbose = result.GetRequiredValue(verbose),
        ExitOnError = result.GetRequiredValue(exitOnError)
    };

    var output = result.GetRequiredValue(outputPath);
    await crawler.CrawlAllMapsAsync(output, cToken);
    Console.WriteLine($"完整本地缓存已保存到 {output}");
});


return await CommandLineParser.Parse(rootCommand, args, new()
{
    EnablePosixBundling = false
}).InvokeAsync(cancellationToken: cTokenSource.Token);



