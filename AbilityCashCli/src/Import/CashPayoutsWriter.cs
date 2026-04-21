using System.Globalization;
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
    private readonly TimeSpan _defaultTime;

    public CashPayoutsWriter(AppDbContext db, CashPayoutsConfig cfg, Type importerType)
    {
        _db = db;
        _cfg = cfg;
        _importerType = importerType;
        if (!TimeSpan.TryParseExact(cfg.DefaultTime, @"h\:mm", CultureInfo.InvariantCulture, out _defaultTime)
            && !TimeSpan.TryParseExact(cfg.DefaultTime, @"hh\:mm", CultureInfo.InvariantCulture, out _defaultTime))
            throw new InvalidOperationException($"CashPayouts.DefaultTime '{cfg.DefaultTime}' не распарсился (ожидается 'HH:mm').");
    }

    public async Task<int> WriteAsync(string source, IReadOnlyList<ImportRecord> records, CancellationToken ct = default)
    {
        if (records.Count == 0) return 0;

        var nowUnix = AbilityCashValues.NowUnix();

        var resolved = new List<(ImportRecord Record, Account Src, Account Dst, int BudgetDate)>();
        var sourceCache = new Dictionary<string, Account>(StringComparer.Ordinal);

        foreach (var r in records)
        {
            var route = ResolveRoute(r.Comment);
            var src = await GetAccountAsync(sourceCache, route.SourceAccount, ct);
            var dstName = _cfg.SalaryAccountPrefix + r.Person;
            var dst = await _db.Accounts.FirstOrDefaultAsync(a => a.Name == dstName && !a.Deleted, ct)
                      ?? throw new InvalidOperationException($"Счёт назначения '{dstName}' не найден.");

            var dateTime = r.Date.TimeOfDay == TimeSpan.Zero ? r.Date.Date + _defaultTime : r.Date;
            var budgetDate = AbilityCashValues.ToUnix(dateTime);

            resolved.Add((r, src, dst, budgetDate));
        }

        var holderUnix = resolved.Min(x => x.BudgetDate);
        var maxPos = await _db.TransactionGroups
            .Where(g => g.HolderDateTime == holderUnix)
            .MaxAsync(g => (int?)g.Position, ct);
        var position = (maxPos ?? -1) + 1;

        var group = new TransactionGroup
        {
            Guid = AbilityCashValues.NewGuidBytes(),
            Changed = nowUnix,
            Deleted = 0,
            HolderDateTime = holderUnix,
            Position = position
        };

        var extra = AbilityCashValues.BuildSourceComment(source, _importerType);

        foreach (var (r, src, dst, budgetDate) in resolved)
        {
            var stored = AbilityCashValues.ToStoredAmount(Math.Abs(r.Amount));
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

        _db.TransactionGroups.Add(group);

        return await _db.SaveChangesAsync(ct);
    }

    private CashPayoutRoute ResolveRoute(string? comment)
    {
        var key = (comment ?? "").Trim();
        foreach (var route in _cfg.Routes)
            if (string.Equals(route.Comment.Trim(), key, StringComparison.OrdinalIgnoreCase))
                return route;
        throw new InvalidOperationException($"Не найден route для комментария '{key}'.");
    }

    private async Task<Account> GetAccountAsync(Dictionary<string, Account> cache, string name, CancellationToken ct)
    {
        if (cache.TryGetValue(name, out var cached)) return cached;
        var acc = await _db.Accounts.FirstOrDefaultAsync(a => a.Name == name && !a.Deleted, ct)
                  ?? throw new InvalidOperationException($"Счёт источника '{name}' не найден.");
        cache[name] = acc;
        return acc;
    }
}
