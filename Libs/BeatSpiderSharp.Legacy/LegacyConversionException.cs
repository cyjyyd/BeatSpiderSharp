namespace BeatSpiderSharp.Legacy;

public class LegacyConversionException(string message, string? targetName = null, Exception? innerException = null)
    : Exception(message, innerException)
{
    public string? TargetName { get; } = targetName;

    public string BaseMessage => base.Message;

    public override string Message =>
        string.IsNullOrWhiteSpace(TargetName) ? base.Message : $"{base.Message} (Target: {TargetName})";
}
