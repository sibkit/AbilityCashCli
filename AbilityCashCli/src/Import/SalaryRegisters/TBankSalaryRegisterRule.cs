namespace AbilityCashCli.Import.SalaryRegisters;

public sealed class TBankSalaryRegisterRule : IImportRule
{
    private readonly IImporter _importer;
    private readonly IImportWriter _writer;

    public TBankSalaryRegisterRule(IImporter importer, IImportWriter writer)
    {
        _importer = importer;
        _writer = writer;
    }

    public bool Matches(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return false;
        return TBankSalaryRegisterImporter.HasHeader(path);
    }

    public async Task<RuleResult> ExecuteAsync(string source, string path, CancellationToken ct = default)
    {
        var records = _importer.Read(path);
        if (records.Count == 0) return new RuleResult(0, 0, Array.Empty<ImportError>());
        var result = await _writer.WriteAsync(source, records, ct);
        return new RuleResult(records.Count, result.Saved, result.Errors);
    }
}
