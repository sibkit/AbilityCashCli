using System.Text.RegularExpressions;

namespace AbilityCashCli.Import;

public static class PersonNameNormalizer
{
    private static readonly Regex DotSpaceRegex = new(@"\.\s+", RegexOptions.Compiled);

    public static string Normalize(string? value) =>
        string.IsNullOrEmpty(value) ? "" : DotSpaceRegex.Replace(value.Trim(), ".");
}
