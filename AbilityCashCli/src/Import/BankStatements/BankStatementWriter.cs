using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;

namespace AbilityCashCli.Import.BankStatements;

public sealed class BankStatementWriter
{
    private readonly AppDbContext _db;

    public BankStatementWriter(AppDbContext db)
    {
        _db = db;
    }

    public async Task<WriterResult> WriteAsync(
        string source,
        Account account,
        IReadOnlyList<BankStatementRow> rows,
        Type importerType,
        CancellationToken ct = default)
    {
        if (rows.Count == 0) return new WriterResult(0, Array.Empty<ImportError>());

        var nowUnix = AbilityCashValues.NowUnix();
        var groups = new TransactionGroupAllocator(_db, nowUnix);
        var extra2 = AbilityCashValues.BuildSourceComment(source, importerType);

        foreach (var r in rows)
        {
            var stored = AbilityCashValues.ToStoredAmount(Math.Abs(r.Amount));
            var budgetDate = AbilityCashValues.StartOfDayUnix(r.Date);
            var extra1 = $"№{r.Number} от {r.ODate:dd.MM.yyyy}, ИНН {r.CounterpartyInn}";

            var group = await groups.NewGroupAsync(budgetDate, ct);

            var txn = new Transaction
            {
                Guid = AbilityCashValues.NewGuidBytes(),
                Changed = nowUnix,
                Deleted = 0,
                Position = 0,
                BudgetDate = budgetDate,
                Executed = 1,
                Locked = 0,
                Quantity = AbilityCashValues.QuantityOne,
                Comment = r.Comment,
                ExtraComment1 = extra1,
                ExtraComment2 = extra2,
                ExtraComment3 = "",
                ExtraComment4 = "",
                BudgetPeriodEnd = budgetDate + AbilityCashValues.DaySeconds
            };

            if (r.DC == "D")
            {
                txn.ExpenseAccount = account.Id;
                txn.ExpenseAmount = -stored;
            }
            else
            {
                txn.IncomeAccount = account.Id;
                txn.IncomeAmount = stored;
            }

            group.Transactions.Add(txn);
        }

        var saved = await _db.SaveChangesAsync(ct);
        return new WriterResult(saved, Array.Empty<ImportError>());
    }
}
