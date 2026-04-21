using System.Text.RegularExpressions;

namespace AbilityCashCli.Import.Vacations;

public sealed class VacationsRule : IImportRule
{
    private static readonly Regex Pattern = new(
        @"^Отпуска.*\.xlsx$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public VacationsRule(IImporter importer, IImportWriter writer)
    {
        Importer = importer;
        Writer = writer;
    }

    public IImporter Importer { get; }

    public IImportWriter Writer { get; }

    public bool Matches(string path) =>
        Pattern.IsMatch(Path.GetFileName(path));
}
