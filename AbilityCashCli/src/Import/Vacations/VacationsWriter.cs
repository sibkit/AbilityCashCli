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
    private readonly Type _importerType;

    public VacationsWriter(
        AppDbContext db,
        VacationConfig cfg,
        CategoryPathResolver resolver,
        TextWriter log,
        Type importerType)
    {
        _db = db;
        _cfg = cfg;
        _resolver = resolver;
        _log = log;
        _importerType = importerType;
    }

    public async Task<WriterResult> WriteAsync(string source, IReadOnlyList<ImportRecord> records, CancellationToken ct = default)
    {
        if (records.Count == 0) return new WriterResult(0, Array.Empty<ImportError>());

        var errors = new List<ImportError>();

        if (!_resolver.TryResolve(_cfg.VacationCategoryPath, out var vacationCategoryId, out var vacErr))
        {
            errors.Add(new ImportError(source, null, "resolve", vacErr!));
            return new WriterResult(0, errors);
        }

        var salaryCategoryIds = new HashSet<int>();
        foreach (var path in _cfg.SalaryCategoryPaths)
        {
            if (_resolver.TryResolve(path, out var id, out var err))
                salaryCategoryIds.Add(id);
            else
                errors.Add(new ImportError(source, null, "resolve", err!));
        }
        if (salaryCategoryIds.Count == 0)
        {
            errors.Add(new ImportError(source, null, "resolve", "VacationConfig.SalaryCategoryPaths пуст или не разрешился."));
            return new WriterResult(0, errors);
        }

        var nowUnix = AbilityCashValues.NowUnix();
        var groups = new TransactionGroupAllocator(_db, nowUnix);
        var extra = AbilityCashValues.BuildSourceComment(source, _importerType);
        var added = 0;

        for (var i = 0; i < records.Count; i++)
        {
            var r = records[i];
            var row = i + 1;

            var accountName = _cfg.SalaryAccountPrefix + r.Person;
            var account = await _db.Accounts
                .FirstOrDefaultAsync(a => a.Name == accountName && !a.Deleted, ct);
            if (account is null)
            {
                errors.Add(new ImportError(source, row, "resolve", $"Счёт '{accountName}' не найден."));
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

            var workStartUnix = await accrualsQuery.MinAsync(t => (int?)t.BudgetDate, ct);
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
                .Where(t => t.BudgetDate >= winStartUnix && t.BudgetDate <= winEndUnix)
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

            var dateUnix = AbilityCashValues.StartOfDayUnix(vacStart.AddDays(-_cfg.PreDaysOffset));

            var txn = new Transaction
            {
                Guid = AbilityCashValues.NewGuidBytes(),
                Changed = nowUnix,
                Deleted = 0,
                Position = 0,
                BudgetDate = dateUnix,
                Executed = 1,
                Locked = 0,
                ExpenseAccount = account.Id,
                ExpenseAmount = -AbilityCashValues.ToStoredAmount(amount),
                Quantity = AbilityCashValues.QuantityOne,
                Comment = r.Comment,
                ExtraComment1 = "",
                ExtraComment2 = extra,
                ExtraComment3 = "",
                ExtraComment4 = "",
                BudgetPeriodEnd = dateUnix + AbilityCashValues.DaySeconds
            };

            txn.TransactionCategories.Add(new TransactionCategory
            {
                Guid = AbilityCashValues.NewGuidBytes(),
                Changed = nowUnix,
                Deleted = 0,
                Category = vacationCategoryId
            });

            var group = await groups.NewGroupAsync(dateUnix, ct);
            group.Transactions.Add(txn);
            added++;
        }

        if (added == 0)
            return new WriterResult(0, errors);

        var saved = await _db.SaveChangesAsync(ct);
        return new WriterResult(saved, errors);
    }
}
