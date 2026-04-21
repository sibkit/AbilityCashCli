using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;
using Microsoft.EntityFrameworkCore;

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
        var holderUnix = rows.Min(r => AbilityCashValues.StartOfDayUnix(r.Date));
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

        var extra2 = AbilityCashValues.BuildSourceComment(source, importerType);

        foreach (var r in rows)
        {
            var stored = AbilityCashValues.ToStoredAmount(Math.Abs(r.Amount));
            var budgetDate = AbilityCashValues.StartOfDayUnix(r.Date);
            var extra1 = $"№{r.Number} от {r.ODate:dd.MM.yyyy}, ИНН {r.CounterpartyInn}";

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
                ExtraComment3 = r.ExtraDoc,
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

        _db.TransactionGroups.Add(group);
        var saved = await _db.SaveChangesAsync(ct);
        return new WriterResult(saved, Array.Empty<ImportError>());
    }
}
