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

    public async Task<(int RowsRead, int RowsSaved)> ExecuteAsync(string source, string path, CancellationToken ct = default)
    {
        var records = _importer.Read(path);
        if (records.Count == 0) return (0, 0);
        var saved = await _writer.WriteAsync(source, records, ct);
        return (records.Count, saved);
    }
}
