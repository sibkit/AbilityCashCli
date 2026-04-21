namespace AbilityCashCli.Import.SalaryRegisters;

public sealed class TBankSalaryRegisterHandler : IImportHandler
{
    private readonly IImporter _importer;
    private readonly IImportWriter _writer;

    public TBankSalaryRegisterHandler(IImporter importer, IImportWriter writer)
    {
        _importer = importer;
        _writer = writer;
    }

    public bool MatchesByName(string path) => false;

    public async Task<HandlerResult?> TryImportAsync(string source, string path, CancellationToken ct = default)
    {
        if (!string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return null;
        if (!TBankSalaryRegisterImporter.HasHeader(path))
            return null;

        var records = _importer.Read(path);
        if (records.Count == 0) return new HandlerResult(0, 0, Array.Empty<ImportError>());
        var result = await _writer.WriteAsync(source, records, ct);
        return new HandlerResult(records.Count, result.Saved, result.Errors);
    }
}
