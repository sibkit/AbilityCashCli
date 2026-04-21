using AbilityCashCli.Configuration;
using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbilityCashCli.Import.Timesheets;

public sealed class TimesheetWriter : IImportWriter
{
    private readonly AppDbContext _db;
    private readonly TimesheetConfig _cfg;
    private readonly IReadOnlyList<SalaryConfig> _salaries;
    private readonly CategoryPathResolver _resolver;
    private readonly Type _importerType;

    public TimesheetWriter(
        AppDbContext db,
        TimesheetConfig cfg,
        IReadOnlyList<SalaryConfig> salaries,
        CategoryPathResolver resolver,
        Type importerType)
    {
        _db = db;
        _cfg = cfg;
        _salaries = salaries;
        _resolver = resolver;
        _importerType = importerType;
    }

    public async Task<WriterResult> WriteAsync(string source, IReadOnlyList<ImportRecord> records, CancellationToken ct = default)
    {
        if (records.Count == 0) return new WriterResult(0, Array.Empty<ImportError>());

        var errors = new List<ImportError>();

        if (!_resolver.TryResolve(_cfg.SalaryCategoryPath, out var categoryId, out var categoryErr))
        {
            errors.Add(new ImportError(source, null, "resolve", categoryErr!));
            return new WriterResult(0, errors);
        }

        var salaryByPerson = _salaries.ToDictionary(s => s.Person, s => s.Amount, StringComparer.Ordinal);

        var nowUnix = AbilityCashValues.NowUnix();
        var budgetDate = AbilityCashValues.ToUnix(records[0].Date);
        var budgetPeriodEnd = budgetDate + AbilityCashValues.DaySeconds;

        var maxPos = await _db.TransactionGroups
            .Where(g => g.HolderDateTime == budgetDate)
            .MaxAsync(g => (int?)g.Position, ct);
        var position = (maxPos ?? -1) + 1;

        var group = new TransactionGroup
        {
            Guid = AbilityCashValues.NewGuidBytes(),
            Changed = nowUnix,
            Deleted = 0,
            HolderDateTime = budgetDate,
            Position = position
        };

        var extra = AbilityCashValues.BuildSourceComment(source, _importerType);

        for (var i = 0; i < records.Count; i++)
        {
            var r = records[i];
            var row = i + 1;

            if (!salaryByPerson.TryGetValue(r.Person, out var salary))
            {
                errors.Add(new ImportError(source, row, "resolve", $"В Salaries нет оклада для '{r.Person}'."));
                continue;
            }

            var accountName = _cfg.SalaryAccountPrefix + r.Person;
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Name == accountName && !a.Deleted, ct);
            if (account is null)
            {
                errors.Add(new ImportError(source, row, "resolve", $"Счёт '{accountName}' не найден."));
                continue;
            }

            var amount = Math.Round(salary * r.Amount, 2, MidpointRounding.AwayFromZero);
            var stored = AbilityCashValues.ToStoredAmount(amount);

            var txn = new Transaction
            {
                Guid = AbilityCashValues.NewGuidBytes(),
                Changed = nowUnix,
                Deleted = 0,
                Position = 0,
                BudgetDate = budgetDate,
                Executed = 1,
                Locked = 0,
                ExpenseAccount = account.Id,
                ExpenseAmount = -stored,
                Quantity = AbilityCashValues.QuantityOne,
                Comment = r.Comment,
                ExtraComment1 = "",
                ExtraComment2 = extra,
                ExtraComment3 = "",
                ExtraComment4 = "",
                BudgetPeriodEnd = budgetPeriodEnd
            };

            txn.TransactionCategories.Add(new TransactionCategory
            {
                Guid = AbilityCashValues.NewGuidBytes(),
                Changed = nowUnix,
                Deleted = 0,
                Category = categoryId
            });

            group.Transactions.Add(txn);
        }

        if (group.Transactions.Count == 0)
            return new WriterResult(0, errors);

        _db.TransactionGroups.Add(group);
        var saved = await _db.SaveChangesAsync(ct);
        return new WriterResult(saved, errors);
    }
}
