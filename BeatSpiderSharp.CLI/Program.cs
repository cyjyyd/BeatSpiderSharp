using System.CommandLine;
using System.CommandLine.Parsing;
using BeatSpiderSharp.CLI.CLIRunners;

var cTokenSource = new CancellationTokenSource();
Console.CancelKeyPress += (o, e) =>
{
    Console.WriteLine("Canceling...");
    e.Cancel = true;
    cTokenSource.Cancel();
};

var rootCommand = new RootCommand();
new BeatSpiderRunner().SetupCommand(rootCommand);

var parsedCommand = CommandLineParser.Parse(rootCommand, args, new()
{
    EnablePosixBundling = false,
});

var code = 0;
try
{
    code = await parsedCommand.InvokeAsync(cancellationToken: cTokenSource.Token);
}
catch (Exception e)
{
    Console.WriteLine("Unhandled exception:");
    Console.WriteLine(e);
    code = -1;
}
finally
{
    Environment.ExitCode = code;
}
