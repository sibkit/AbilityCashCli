using AbilityCashCli.Data;

namespace AbilityCashCli.Import;

public sealed class BulkImportRunner
{
    private readonly AppDbContext _db;
    private readonly IImportRouter _router;
    private readonly IImportArchiver _archiver;
    private readonly TextWriter _log;

    public BulkImportRunner(
        AppDbContext db,
        IImportRouter router,
        IImportArchiver archiver,
        TextWriter log)
    {
        _db = db;
        _router = router;
        _archiver = archiver;
        _log = log;
    }

    public async Task<Summary> RunAsync(IEnumerable<string> files, CancellationToken ct = default)
    {
        var imported = new List<string>();
        var skipped = new List<(string Path, string Reason)>();
        var errored = new List<(string Path, string Reason)>();

        await using var trx = await _db.Database.BeginTransactionAsync(ct);

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
                var source = Path.GetFileName(path);
                var (rows, saved) = await rule.ExecuteAsync(source, path, ct);
                if (rows == 0)
                {
                    skipped.Add((path, "пусто"));
                    _log.WriteLine($"skip (пусто): {path}");
                    continue;
                }

                _log.WriteLine($"ok: {path} (rows={rows}, saved={saved})");
                imported.Add(path);
            }
            catch (Exception ex)
            {
                errored.Add((path, ex.Message));
                WriteError($"error: {path} :: {ex}");
            }
        }

        if (errored.Count == 0)
        {
            await trx.CommitAsync(ct);
            foreach (var p in imported)
            {
                var archived = _archiver.Archive(p);
                _log.WriteLine($"archived: {p} -> {archived}");
            }
        }
        else
        {
            WriteError("Есть ошибки — транзакция откатывается, архивация пропущена.");
        }

        _log.WriteLine();
        _log.WriteLine($"Summary: imported={imported.Count}, skipped={skipped.Count}, errored={errored.Count}");
        foreach (var p in imported)
            _log.WriteLine($"  imported: {Path.GetFileName(p)}");
        foreach (var (p, reason) in skipped)
            _log.WriteLine($"  skipped:  {Path.GetFileName(p)} ({reason})");
        foreach (var (p, reason) in errored)
            WriteError($"  errored:  {Path.GetFileName(p)} ({reason})");

        return new Summary(imported.Count, skipped.Count, errored.Count);
    }

    private static void WriteError(string message)
    {
        var prev = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;
        try
        {
            Console.Error.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = prev;
        }
    }

    public sealed record Summary(int Imported, int Skipped, int Errored);
}
