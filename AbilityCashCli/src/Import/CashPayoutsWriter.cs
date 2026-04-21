using AbilityCashCli.Configuration;
using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbilityCashCli.Import;

public sealed class CashPayoutsWriter : IImportWriter
{
    private readonly AppDbContext _db;
    private readonly CashPayoutsConfig _cfg;
    private readonly Type _importerType;

    public CashPayoutsWriter(AppDbContext db, CashPayoutsConfig cfg, Type importerType)
    {
        _db = db;
        _cfg = cfg;
        _importerType = importerType;
    }

    public async Task<WriterResult> WriteAsync(string source, IReadOnlyList<ImportRecord> records, CancellationToken ct = default)
    {
        if (records.Count == 0) return new WriterResult(0, Array.Empty<ImportError>());

        var nowUnix = AbilityCashValues.NowUnix();
        var errors = new List<ImportError>();
        var resolved = new List<(ImportRecord Record, Account Src, Account Dst, int BudgetDate)>();
        var sourceCache = new Dictionary<string, Account>(StringComparer.Ordinal);

        for (var i = 0; i < records.Count; i++)
        {
            var r = records[i];
            var row = i + 1;
            var err = await TryResolveRowAsync(source, row, r, sourceCache, resolved, ct);
            if (err is not null) errors.Add(err);
        }

        if (resolved.Count == 0)
            return new WriterResult(0, errors);

        var groups = new TransactionGroupAllocator(_db, nowUnix);
        var extra = AbilityCashValues.BuildSourceComment(source, _importerType);

        foreach (var (r, src, dst, budgetDate) in resolved)
        {
            var stored = AbilityCashValues.ToStoredAmount(Math.Abs(r.Amount));
            var group = await groups.NewGroupAsync(budgetDate, ct);
            group.Transactions.Add(new Transaction
            {
                Guid = AbilityCashValues.NewGuidBytes(),
                Changed = nowUnix,
                Deleted = 0,
                Position = 0,
                BudgetDate = budgetDate,
                Executed = 1,
                Locked = 0,
                ExpenseAccount = src.Id,
                ExpenseAmount = -stored,
                IncomeAccount = dst.Id,
                IncomeAmount = stored,
                Quantity = AbilityCashValues.QuantityOne,
                Comment = r.Comment,
                ExtraComment1 = "",
                ExtraComment2 = extra,
                ExtraComment3 = "",
                ExtraComment4 = "",
                BudgetPeriodEnd = budgetDate + AbilityCashValues.DaySeconds
            });
        }

        var saved = await _db.SaveChangesAsync(ct);
        return new WriterResult(saved, errors);
    }

    private async Task<ImportError?> TryResolveRowAsync(
        string source,
        int row,
        ImportRecord r,
        Dictionary<string, Account> sourceCache,
        List<(ImportRecord, Account, Account, int)> resolved,
        CancellationToken ct)
    {
        var route = ResolveRoute(r.Comment);
        if (route is null)
            return new ImportError(source, row, "resolve",
                $"Не найден route для комментария '{(r.Comment ?? string.Empty).Trim()}'.");

        var src = await GetAccountAsync(sourceCache, route.SourceAccount, ct);
        if (src is null)
            return new ImportError(source, row, "resolve", $"Счёт источника '{route.SourceAccount}' не найден.");

        var dstName = _cfg.SalaryAccountPrefix + r.Person;
        var dst = await _db.Accounts.FirstOrDefaultAsync(a => a.Name == dstName && !a.Deleted, ct);
        if (dst is null)
            return new ImportError(source, row, "resolve", $"Счёт назначения '{dstName}' не найден.");

        var budgetDate = AbilityCashValues.StartOfDayUnix(r.Date);

        resolved.Add((r, src, dst, budgetDate));
        return null;
    }

    private CashPayoutRoute? ResolveRoute(string? comment)
    {
        var key = (comment ?? "").Trim();
        foreach (var route in _cfg.Routes)
            if (string.Equals(route.Comment.Trim(), key, StringComparison.OrdinalIgnoreCase))
                return route;
        return null;
    }

    private async Task<Account?> GetAccountAsync(Dictionary<string, Account> cache, string name, CancellationToken ct)
    {
        if (cache.TryGetValue(name, out var cached)) return cached;
        var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.Name == name && !a.Deleted, ct);
        if (acc is null) return null;
        cache[name] = acc;
        return acc;
    }
}
