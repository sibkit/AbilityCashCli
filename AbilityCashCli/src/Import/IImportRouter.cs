namespace AbilityCashCli.Import;

public interface IImportRouter
{
    Task<HandlerResult?> ImportAsync(string source, string path, CancellationToken ct = default);
}
