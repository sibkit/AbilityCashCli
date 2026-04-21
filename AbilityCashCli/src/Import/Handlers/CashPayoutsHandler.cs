using System.Text.RegularExpressions;

namespace AbilityCashCli.Import.Handlers;

public sealed class CashPayoutsHandler : IImportHandler
{
    private static readonly Regex Pattern = new(
        @"^Выплаты наличные.*\.xls$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IImporter _importer;
    private readonly IImportWriter _writer;

    public CashPayoutsHandler(IImporter importer, IImportWriter writer)
    {
        _importer = importer;
        _writer = writer;
    }

    public bool MatchesByName(string path) => false;

    public async Task<HandlerResult?> TryImportAsync(string source, string path, CancellationToken ct = default)
    {
        if (!Pattern.IsMatch(Path.GetFileName(path))) return null;

        var records = _importer.Read(path);
        if (records.Count == 0) return new HandlerResult(0, 0, Array.Empty<ImportError>());
        var result = await _writer.WriteAsync(source, records, ct);
        return new HandlerResult(records.Count, result.Saved, result.Errors);
    }
}