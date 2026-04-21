namespace AbilityCashCli.Import.BankStatements;

public sealed class AlfaBankRule : IImportRule
{
    private readonly AlfaBankImporter _importer;
    private readonly BankStatementWriter _writer;

    public AlfaBankRule(AlfaBankImporter importer, BankStatementWriter writer)
    {
        _importer = importer;
        _writer = writer;
    }

    public bool Matches(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
            return false;
        return AlfaBankImporter.HasHeader(path);
    }

    public async Task<RuleResult> ExecuteAsync(string source, string path, CancellationToken ct = default)
    {
        var records = _importer.Read(path);
        if (records.Count == 0) return new RuleResult(0, 0, Array.Empty<ImportError>());
        var result = await _writer.WriteAsync(source, records, ct);
        return new RuleResult(records.Count, result.Saved, result.Errors);
    }
}
