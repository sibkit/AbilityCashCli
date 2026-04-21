using System.Text.RegularExpressions;

namespace AbilityCashCli.Import.Rules;

public sealed class CashPayoutsRule : IImportRule
{
    private static readonly Regex Pattern = new(
        @"^Выплаты наличные.*\.xls$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public CashPayoutsRule(IImporter importer)
    {
        Importer = importer;
    }

    public IImporter Importer { get; }

    public bool Matches(string path) =>
        Pattern.IsMatch(Path.GetFileName(path));
}
