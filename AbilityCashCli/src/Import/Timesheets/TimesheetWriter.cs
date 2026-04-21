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

    public async Task<int> WriteAsync(string source, IReadOnlyList<ImportRecord> records, CancellationToken ct = default)
    {
        if (records.Count == 0) return 0;

        var categoryId = _resolver.Resolve(_cfg.SalaryCategoryPath);
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

        foreach (var r in records)
        {
            if (!salaryByPerson.TryGetValue(r.Person, out var salary))
                throw new InvalidOperationException($"В Salaries нет оклада для '{r.Person}'.");

            var accountName = _cfg.SalaryAccountPrefix + r.Person;
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Name == accountName && !a.Deleted, ct)
                          ?? throw new InvalidOperationException($"Счёт '{accountName}' не найден.");

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

        _db.TransactionGroups.Add(group);

        return await _db.SaveChangesAsync(ct);
    }
}
