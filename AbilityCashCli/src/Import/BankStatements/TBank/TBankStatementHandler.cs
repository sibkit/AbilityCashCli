using System.Text.RegularExpressions;

namespace AbilityCashCli.Import.BankStatements.TBank;

public sealed class TBankStatementHandler : IImportHandler
{
    private static readonly Regex AccountInName = new(@"\d{20}", RegexOptions.Compiled);
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly TBankStatementImporter _importer;
    private readonly IBankAccountResolver _resolver;
    private readonly BankStatementWriter _writer;

    public TBankStatementHandler(TBankStatementImporter importer, IBankAccountResolver resolver, BankStatementWriter writer)
    {
        _importer = importer;
        _resolver = resolver;
        _writer = writer;
    }

    public bool MatchesByName(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
            return false;
        return AccountInName.IsMatch(Path.GetFileNameWithoutExtension(path));
    }

    public async Task<HandlerResult?> TryImportAsync(string source, string path, CancellationToken ct = default)
    {
        if (!string.Equals(Path.GetExtension(path), ".csv", StringComparison.OrdinalIgnoreCase))
            return null;
        if (!TBankStatementImporter.HasHeader(path))
            return null;

        var records = _importer.Read(path);
        if (records.Count == 0) return new HandlerResult(0, 0, Array.Empty<ImportError>());

        var account = await _resolver.ResolveAsync(records[0].Account, ct);

        var rows = new List<BankStatementRow>(records.Count);
        foreach (var r in records)
        {
            var counterparty = Normalize(r.CounterpartyName);
            var purpose = Normalize(r.Purpose);
            rows.Add(new BankStatementRow(
                Date: r.Date,
                DC: r.DC,
                Amount: r.AmountRur,
                Number: r.Number,
                ODate: r.Date,
                Comment: $"[{counterparty}] {purpose}",
                CounterpartyName: counterparty,
                CounterpartyInn: r.CounterpartyInn,
                CounterpartyAcc: r.CounterpartyAcc));
        }

        var result = await _writer.WriteAsync(source, account, rows, _importer.GetType(), ct);
        return new HandlerResult(records.Count, result.Saved, result.Errors);
    }

    private static string Normalize(string value) =>
        WhitespaceRegex.Replace(value.Trim(), " ");
}
