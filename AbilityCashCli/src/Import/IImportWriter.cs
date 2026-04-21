namespace AbilityCashCli.Import;

public interface IImportWriter
{
    Task<int> WriteAsync(string source, IReadOnlyList<ImportRecord> records, CancellationToken ct = default);
}
