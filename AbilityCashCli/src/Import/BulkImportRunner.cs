using AbilityCashCli.Cli;
using AbilityCashCli.Data;

namespace AbilityCashCli.Import;

public sealed class BulkImportRunner
{
    private readonly AppDbContext _db;
    private readonly IImportRouter _router;
    private readonly IImportArchiver _archiver;
    private readonly ImportReportWriter _report;
    private readonly AccountBalanceRecalculator _balances;

    public BulkImportRunner(
        AppDbContext db,
        IImportRouter router,
        IImportArchiver archiver,
        ImportReportWriter report,
        AccountBalanceRecalculator balances)
    {
        _db = db;
        _router = router;
        _archiver = archiver;
        _report = report;
        _balances = balances;
    }

    public async Task<Summary> RunAsync(IEnumerable<string> files, CancellationToken ct = default)
    {
        var imported = new List<string>();
        var erroredFiles = new List<string>();

        await using var trx = await _db.Database.BeginTransactionAsync(ct);

        foreach (var path in files)
        {
            var source = Path.GetFileName(path);

            HandlerResult? result;
            try
            {
                result = await _router.ImportAsync(source, path, ct);
            }
            catch (Exception ex)
            {
                var fatal = new[] { new ImportError(source, null, "parse", ex.Message) };
                _report.FileErrors(path, fatal);
                erroredFiles.Add(path);
                continue;
            }

            if (result is null)
            {
                _report.FileErrors(path, new[] { new ImportError(source, null, "route", "нет importer'а") });
                erroredFiles.Add(path);
                continue;
            }

            if (result.Errors.Count > 0)
            {
                _report.FileErrors(path, result.Errors);
                erroredFiles.Add(path);
                continue;
            }

            if (result.RowsRead == 0)
            {
                _report.FileErrors(path, new[] { new ImportError(source, null, "import", "пусто") });
                erroredFiles.Add(path);
                continue;
            }

            imported.Add(path);
            _report.Ok(path, result.RowsRead, result.RowsSaved);
        }

        if (erroredFiles.Count == 0)
        {
            if (imported.Count > 0)
                await _balances.RecalculateAsync(ct);

            await trx.CommitAsync(ct);
            foreach (var p in imported)
            {
                var archived = _archiver.Archive(p);
                _report.Info($"archived: {p} -> {archived}");
            }
        }
        else
        {
            _report.RollbackNotice();
        }

        _report.BlankLine();
        _report.Info($"Summary: imported={imported.Count}, errored={erroredFiles.Count}");
        foreach (var p in imported)
            _report.Info($"  imported: {Path.GetFileName(p)}");
        foreach (var p in erroredFiles)
            _report.ErrorLine($"  errored:  {Path.GetFileName(p)}");

        return new Summary(imported.Count, erroredFiles.Count);
    }

    public sealed record Summary(int Imported, int Errored);
}
