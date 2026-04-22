using System.Text.RegularExpressions;

namespace AbilityCashCli.Import.SalaryRegisters;

public sealed class AlfaBankSalaryRegisterHandler : IImportHandler
{
    private static readonly Regex Pattern = new(
        @"^Реестр начислений.*\.xls$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IImporter _importer;
    private readonly IImportWriter _writer;

    public AlfaBankSalaryRegisterHandler(IImporter importer, IImportWriter writer)
    {
        _importer = importer;
        _writer = writer;
    }

    public bool MatchesByName(string path) => Pattern.IsMatch(Path.GetFileName(path));

    public async Task<HandlerResult?> TryImportAsync(string source, string path, CancellationToken ct = default)
    {
        if (!MatchesByName(path)) return null;

        var records = _importer.Read(path);
        if (records.Count == 0) return new HandlerResult(0, 0, Array.Empty<ImportError>());
        var result = await _writer.WriteAsync(source, records, ct);
        return new HandlerResult(records.Count, result.Saved, result.Errors);
    }
}
