using System.Text;
using System.Text.RegularExpressions;
using AbilityCashCli.Configuration;

namespace AbilityCashCli.Import;

public sealed class PersonNameNormalizer
{
    private static readonly Regex DotSpaceRegex = new(@"\.\s+", RegexOptions.Compiled);
    private static readonly char[] WhitespaceSeparators = [' ', '\t'];

    private readonly IReadOnlyDictionary<string, string> _aliases;

    public PersonNameNormalizer(IReadOnlyList<PersonNameAliasConfig>? aliases)
    {
        _aliases = (aliases ?? Array.Empty<PersonNameAliasConfig>())
            .ToDictionary(a => a.From, a => a.To, StringComparer.Ordinal);
    }

    public string Normalize(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        var trimmed = value.Trim();
        var tokens = trimmed.Split(WhitespaceSeparators, StringSplitOptions.RemoveEmptyEntries);

        string result;
        if (tokens.Length >= 3)
        {
            var surname = TitleCase(string.Join(' ', tokens.Take(tokens.Length - 2)));
            var init1 = ToInitial(tokens[^2]).ToUpperInvariant();
            var init2 = ToInitial(tokens[^1]).ToUpperInvariant();
            result = $"{surname} {init1}{init2}";
        }
        else
        {
            result = string.Join(' ', tokens.Select(NormalizeShortToken));
        }

        result = DotSpaceRegex.Replace(result, ".");

        return _aliases.TryGetValue(result, out var alias) ? alias : result;
    }

    private static string NormalizeShortToken(string token) =>
        token.Contains('.') ? token.ToUpperInvariant() : TitleCase(token);

    private static string ToInitial(string token) =>
        token.Contains('.') ? token : token[..1] + ".";

    private static string TitleCase(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        var sb = new StringBuilder(value.Length);
        var prevIsLetter = false;
        foreach (var ch in value)
        {
            var isLetter = char.IsLetter(ch);
            if (isLetter && !prevIsLetter) sb.Append(char.ToUpperInvariant(ch));
            else if (isLetter) sb.Append(char.ToLowerInvariant(ch));
            else sb.Append(ch);
            prevIsLetter = isLetter;
        }
        return sb.ToString();
    }
}
