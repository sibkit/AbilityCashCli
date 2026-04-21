namespace AbilityCashCli.Import;

public interface IImportWriter
{
    Task<WriterResult> WriteAsync(string source, IReadOnlyList<ImportRecord> records, CancellationToken ct = default);
}
