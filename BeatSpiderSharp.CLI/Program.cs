using System.CommandLine;
using System.CommandLine.Parsing;
using BeatSpiderSharp.CLI.CLIRunners;

var rootCommand = new RootCommand();
new BeatSpiderRunner().SetupCommand(rootCommand);

var commandLineOptions = new CommandLineConfiguration(rootCommand)
{
    EnablePosixBundling = false,
    EnableDefaultExceptionHandler = false
};

var parsedCommand = CommandLineParser.Parse(rootCommand, args, commandLineOptions);

var code = 0;
try
{
    code = await parsedCommand.InvokeAsync();
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
