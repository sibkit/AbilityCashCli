using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbilityCashCli.Import;

public sealed class CashPayoutsWriter : IImportWriter
{
    private readonly AppDbContext _db;
    private readonly string _accountName;

    public CashPayoutsWriter(AppDbContext db, string accountName)
    {
        _db = db;
        _accountName = accountName;
    }

    public async Task<int> WriteAsync(string source, IReadOnlyList<ImportRecord> records, CancellationToken ct = default)
    {
        if (records.Count == 0) return 0;

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.Name == _accountName && !a.Deleted, ct);
        if (account is null)
            throw new InvalidOperationException($"Счёт '{_accountName}' не найден в Accounts.Name (или помечен Deleted).");

        var holderUnix = records.Min(r => AbilityCashValues.StartOfDayUnix(r.Date));
        var nowUnix = AbilityCashValues.NowUnix();

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

        foreach (var r in records)
        {
            var dateUnix = AbilityCashValues.StartOfDayUnix(r.Date);
            var comment = string.IsNullOrEmpty(r.Comment)
                ? r.Person
                : $"{r.Person} — {r.Comment}";

            group.Transactions.Add(new Transaction
            {
                Guid = AbilityCashValues.NewGuidBytes(),
                Changed = nowUnix,
                Deleted = 0,
                Position = 0,
                BudgetDate = dateUnix,
                Executed = 1,
                Locked = 0,
                ExpenseAccount = account.Id,
                ExpenseAmount = -AbilityCashValues.ToStoredAmount(Math.Abs(r.Amount)),
                Quantity = AbilityCashValues.QuantityOne,
                Comment = comment,
                ExtraComment1 = "",
                ExtraComment2 = source,
                ExtraComment3 = "",
                ExtraComment4 = "",
                BudgetPeriodEnd = dateUnix + AbilityCashValues.DaySeconds
            });
        }

        _db.TransactionGroups.Add(group);

        await using var trx = await _db.Database.BeginTransactionAsync(ct);
        var saved = await _db.SaveChangesAsync(ct);
        await trx.CommitAsync(ct);
        return saved;
    }
}
