using AbilityCashCli.Configuration;
using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbilityCashCli.Import.Vacations;

public sealed class VacationsWriter : IImportWriter
{
    private readonly AppDbContext _db;
    private readonly VacationConfig _cfg;
    private readonly CategoryPathResolver _resolver;
    private readonly TextWriter _log;

    public VacationsWriter(AppDbContext db, VacationConfig cfg, CategoryPathResolver resolver, TextWriter log)
    {
        _db = db;
        _cfg = cfg;
        _resolver = resolver;
        _log = log;
    }

    public async Task<int> WriteAsync(string source, IReadOnlyList<ImportRecord> records, CancellationToken ct = default)
    {
        if (records.Count == 0) return 0;

        var vacationCategoryId = _resolver.Resolve(_cfg.VacationCategoryPath);
        var salaryCategoryIds = _cfg.SalaryCategoryPaths.Select(_resolver.Resolve).ToHashSet();
        if (salaryCategoryIds.Count == 0)
            throw new InvalidOperationException("VacationConfig.SalaryCategoryPaths пуст.");

        var nowUnix = AbilityCashValues.NowUnix();
        var maxPos = await _db.TransactionGroups
            .Where(g => g.HolderDateTime == nowUnix)
            .MaxAsync(g => (int?)g.Position, ct);
        var position = (maxPos ?? -1) + 1;

        var group = new TransactionGroup
        {
            Guid = AbilityCashValues.NewGuidBytes(),
            Changed = nowUnix,
            Deleted = 0,
            HolderDateTime = nowUnix,
            Position = position
        };

        foreach (var r in records)
        {
            var accountName = _cfg.SalaryAccountPrefix + r.Person;
            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.Name == accountName && !a.Deleted, ct);
            if (account is null)
            {
                _log.WriteLine($"  skip: счёт '{accountName}' не найден");
                continue;
            }

            var vacStart = r.Date;
            var days = (int)r.Amount;

            var accrualsQuery = _db.Transactions
                .Where(t => t.ExpenseAccount == account.Id
                            && t.IncomeAccount == null
                            && t.Deleted == 0
                            && t.TransactionCategories.Any(tc =>
                                salaryCategoryIds.Contains(tc.Category) && tc.Deleted == 0));

            var workStartUnix = await accrualsQuery.MinAsync(t => (int?)t.Executed, ct);
            if (workStartUnix is null)
            {
                _log.WriteLine($"  skip: нет начислений для '{accountName}'");
                continue;
            }

            var workStart = DateTimeOffset.FromUnixTimeSeconds(workStartUnix.Value).UtcDateTime.Date;

            var windowEnd = new DateTime(vacStart.Year, vacStart.Month, 1).AddDays(-1);
            var windowStart = windowEnd.AddDays(1).AddMonths(-12);

            var workStartMonth = new DateTime(workStart.Year, workStart.Month, 1);
            if (workStart.Day > 1) workStartMonth = workStartMonth.AddMonths(1);

            var effectiveWindowStart = workStartMonth > windowStart ? workStartMonth : windowStart;

            var monthsCount = (windowEnd.Year - effectiveWindowStart.Year) * 12
                              + (windowEnd.Month - effectiveWindowStart.Month) + 1;
            if (monthsCount <= 0)
            {
                _log.WriteLine($"  skip: окно начислений пустое для '{accountName}'");
                continue;
            }

            var winStartUnix = AbilityCashValues.StartOfDayUnix(effectiveWindowStart);
            var winEndUnix = AbilityCashValues.StartOfDayUnix(windowEnd) + AbilityCashValues.DaySeconds - 1;

            var sumStored = await accrualsQuery
                .Where(t => t.Executed >= winStartUnix && t.Executed <= winEndUnix)
                .SumAsync(t => t.ExpenseAmount ?? 0L, ct);
            var sumStoredAbs = Math.Abs(sumStored);
            if (sumStoredAbs == 0)
            {
                _log.WriteLine($"  skip: сумма начислений = 0 для '{accountName}'");
                continue;
            }

            var sumHuman = (decimal)sumStoredAbs / AbilityCashValues.MoneyMultiplier;
            var avgMonth = sumHuman / monthsCount;
            var amount = avgMonth / _cfg.AverageDaysPerMonth * days;

            var executedUnix = AbilityCashValues.StartOfDayUnix(vacStart.AddDays(-_cfg.PreDaysOffset));

            var txn = new Transaction
            {
                Guid = AbilityCashValues.NewGuidBytes(),
                Changed = nowUnix,
                Deleted = 0,
                Position = 0,
                BudgetDate = executedUnix,
                Executed = executedUnix,
                Locked = 0,
                ExpenseAccount = account.Id,
                ExpenseAmount = -AbilityCashValues.ToStoredAmount(amount),
                Quantity = AbilityCashValues.QuantityOne,
                Comment = r.Comment,
                ExtraComment1 = "",
                ExtraComment2 = source,
                ExtraComment3 = "",
                ExtraComment4 = "",
                BudgetPeriodEnd = executedUnix + AbilityCashValues.DaySeconds
            };

            txn.TransactionCategories.Add(new TransactionCategory
            {
                Guid = AbilityCashValues.NewGuidBytes(),
                Changed = nowUnix,
                Deleted = 0,
                Category = vacationCategoryId
            });

            group.Transactions.Add(txn);
        }

        if (group.Transactions.Count == 0)
            return 0;

        _db.TransactionGroups.Add(group);

        await using var trx = await _db.Database.BeginTransactionAsync(ct);
        var saved = await _db.SaveChangesAsync(ct);
        await trx.CommitAsync(ct);
        return saved;
    }
}
