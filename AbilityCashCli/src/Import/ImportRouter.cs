namespace AbilityCashCli.Import;

public sealed class ImportRouter : IImportRouter
{
    private readonly IReadOnlyList<IImportHandler> _handlers;

    public ImportRouter(IEnumerable<IImportHandler> handlers)
    {
        _handlers = handlers.ToList();
    }

    public async Task<HandlerResult?> ImportAsync(string source, string path, CancellationToken ct = default)
    {
        var tried = new HashSet<IImportHandler>();

        foreach (var handler in _handlers)
        {
            if (!handler.MatchesByName(path)) continue;
            tried.Add(handler);
            var result = await handler.TryImportAsync(source, path, ct);
            if (result is not null) return result;
        }

        foreach (var handler in _handlers)
        {
            if (tried.Contains(handler)) continue;
            var result = await handler.TryImportAsync(source, path, ct);
            if (result is not null) return result;
        }

        return null;
    }
}
