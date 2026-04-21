namespace AbilityCashCli.Import;

public sealed class BulkImportRunner
{
    private readonly IImportRouter _router;
    private readonly TransactionWriter _writer;
    private readonly IImportArchiver _archiver;
    private readonly TextWriter _log;

    public BulkImportRunner(
        IImportRouter router,
        TransactionWriter writer,
        IImportArchiver archiver,
        TextWriter log)
    {
        _router = router;
        _writer = writer;
        _archiver = archiver;
        _log = log;
    }

    public async Task<int> RunAsync(IEnumerable<string> files, string accountName, CancellationToken ct = default)
    {
        var imported = 0;

        foreach (var path in files)
        {
            var importer = _router.Resolve(path);
            if (importer is null)
            {
                _log.WriteLine($"skip (нет importer'a): {path}");
                continue;
            }

            try
            {
                var records = importer.Read(path);
                if (records.Count == 0)
                {
                    _log.WriteLine($"skip (пусто): {path}");
                    continue;
                }

                var source = Path.GetFileName(path);
                var saved = await _writer.WriteAsync(accountName, source, records, ct);
                var archived = _archiver.Archive(path);
                _log.WriteLine($"ok: {path} -> {archived} (rows={records.Count}, saved={saved})");
                imported++;
            }
            catch (Exception ex)
            {
                _log.WriteLine($"error: {path} :: {ex.Message}");
            }
        }

        return imported;
    }
}
