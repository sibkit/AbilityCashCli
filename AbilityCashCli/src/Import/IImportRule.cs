namespace AbilityCashCli.Import;

public interface IImportRule
{
    bool Matches(string path);

    Task<(int RowsRead, int RowsSaved)> ExecuteAsync(string source, string path, CancellationToken ct = default);
}
