using System.Text.RegularExpressions;

namespace AbilityCashCli.Import.Timesheets;

public sealed class TimesheetRule : IImportRule
{
    private static readonly Regex Pattern = new(
        @"^Табель\s.*\.xlsx?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IImporter _importer;
    private readonly IImportWriter _writer;

    public TimesheetRule(IImporter importer, IImportWriter writer)
    {
        _importer = importer;
        _writer = writer;
    }

    public bool Matches(string path) =>
        Pattern.IsMatch(Path.GetFileName(path));

    public async Task<RuleResult> ExecuteAsync(string source, string path, CancellationToken ct = default)
    {
        var records = _importer.Read(path);
        if (records.Count == 0) return new RuleResult(0, 0, Array.Empty<ImportError>());
        var result = await _writer.WriteAsync(source, records, ct);
        return new RuleResult(records.Count, result.Saved, result.Errors);
    }
}
