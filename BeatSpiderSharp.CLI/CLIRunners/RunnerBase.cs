using System.CommandLine;

namespace BeatSpiderSharp.CLI.CLIRunners;

internal abstract class RunnerBase
{
    protected abstract string Description { get; }
    protected virtual Option[] Options { get; } = [];

    protected abstract Task<int> RunAsync(ParseResult parseResult, CancellationToken cancellationToken);

    public void SetupCommand(Command command)
    {
        command.Description = Description;
        command.TreatUnmatchedTokensAsErrors = true;
        foreach (var option in Options)
        {
            command.Options.Add(option);
        }

        command.SetAction(RunAsync);
    }
}
