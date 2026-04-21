namespace AbilityCashCli.Import;

public sealed class BulkImportRunner
{
    private readonly IImportRouter _router;
    private readonly IImportArchiver _archiver;
    private readonly TextWriter _log;

    public BulkImportRunner(
        IImportRouter router,
        IImportArchiver archiver,
        TextWriter log)
    {
        _router = router;
        _archiver = archiver;
        _log = log;
    }

    public async Task<Summary> RunAsync(IEnumerable<string> files, CancellationToken ct = default)
    {
        var imported = new List<string>();
        var skipped = new List<(string Path, string Reason)>();
        var errored = new List<(string Path, string Reason)>();

        foreach (var path in files)
        {
            var rule = _router.Resolve(path);
            if (rule is null)
            {
                skipped.Add((path, "нет importer'а"));
                _log.WriteLine($"skip (нет importer'а): {path}");
                continue;
            }

            try
            {
                var records = rule.Importer.Read(path);
                if (records.Count == 0)
                {
                    skipped.Add((path, "пусто"));
                    _log.WriteLine($"skip (пусто): {path}");
                    continue;
                }

                var source = Path.GetFileName(path);
                var saved = await rule.Writer.WriteAsync(source, records, ct);
                var archived = _archiver.Archive(path);
                _log.WriteLine($"ok: {path} -> {archived} (rows={records.Count}, saved={saved})");
                imported.Add(path);
            }
            catch (Exception ex)
            {
                errored.Add((path, ex.Message));
                _log.WriteLine($"error: {path} :: {ex.Message}");
            }
        }

        _log.WriteLine();
        _log.WriteLine($"Summary: imported={imported.Count}, skipped={skipped.Count}, errored={errored.Count}");
        foreach (var p in imported)
            _log.WriteLine($"  imported: {Path.GetFileName(p)}");
        foreach (var (p, reason) in skipped)
            _log.WriteLine($"  skipped:  {Path.GetFileName(p)} ({reason})");
        foreach (var (p, reason) in errored)
            _log.WriteLine($"  errored:  {Path.GetFileName(p)} ({reason})");

        return new Summary(imported.Count, skipped.Count, errored.Count);
    }

    public sealed record Summary(int Imported, int Skipped, int Errored);
}
