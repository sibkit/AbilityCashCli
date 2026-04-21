namespace AbilityCashCli.Import;

public interface IImportHandler
{
    bool MatchesByName(string path);

    Task<HandlerResult?> TryImportAsync(string source, string path, CancellationToken ct = default);
}