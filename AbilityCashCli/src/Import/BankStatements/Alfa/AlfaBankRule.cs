using System.Text.RegularExpressions;

namespace AbilityCashCli.Import.BankStatements.Alfa;

public sealed class AlfaBankRule : IImportRule
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly AlfaBankImporter _importer;
    private readonly IBankAccountResolver _resolver;
    private readonly BankStatementWriter _writer;

    public AlfaBankRule(AlfaBankImporter importer, IBankAccountResolver resolver, BankStatementWriter writer)
    {
        _importer = importer;
        _resolver = resolver;
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

        var account = await _resolver.ResolveAsync(records[0].Rch, ct);

        var rows = new List<BankStatementRow>(records.Count);
        foreach (var r in records)
        {
            var counterparty = Normalize(r.CounterpartyName);
            var text70 = Normalize(r.Text70);
            rows.Add(new BankStatementRow(
                Date: r.Date,
                DC: r.DC,
                Amount: r.AmountRur,
                Number: r.Number,
                ODate: r.ODate,
                Comment: $"[{counterparty}] {text70}",
                CounterpartyName: counterparty,
                CounterpartyInn: r.CounterpartyInn,
                CounterpartyAcc: r.CounterpartyAcc,
                ExtraDoc: ""));
        }

        var result = await _writer.WriteAsync(source, account, rows, _importer.GetType(), ct);
        return new RuleResult(records.Count, result.Saved, result.Errors);
    }

    private static string Normalize(string value) =>
        WhitespaceRegex.Replace(value.Trim(), " ");
}
