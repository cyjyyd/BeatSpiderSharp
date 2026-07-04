using System.Buffers;

namespace BeatSpiderSharp.Extensions;

public static class StringExtensions
{
    private static readonly SearchValues<char> InvalidFileNameChars =
        SearchValues.Create(Path.GetInvalidFileNameChars());

    public static bool IsHex(this string str)
    {
        return str.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
    }

    public static string SanitizeFileName(this string fileName)
    {
        return string.Concat(fileName.Select(c => InvalidFileNameChars.Contains(c) ? '_' : c));
    }
}
