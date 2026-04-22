using AbilityCashCli.Data;
using Microsoft.EntityFrameworkCore;

namespace AbilityCashCli.Import;

public sealed class AccountBalanceRecalculator
{
    private readonly AppDbContext _db;

    public AccountBalanceRecalculator(AppDbContext db)
    {
        _db = db;
    }

    public async Task RecalculateAsync(CancellationToken ct = default)
    {
        var running = await _db.Accounts
            .ToDictionaryAsync(a => a.Id, a => a.StartingBalance, ct);

        var transactions = await _db.Transactions
            .Include(t => t.GroupNavigation)
            .Where(t => t.Deleted == 0 && t.Executed != 0)
            .OrderBy(t => t.GroupNavigation.HolderDateTime)
            .ThenBy(t => t.GroupNavigation.Position)
            .ThenBy(t => t.Position)
            .ThenBy(t => t.Id)
            .ToListAsync(ct);

        foreach (var t in transactions)
        {
            if (t.IncomeAccount is { } inc)
            {
                var balance = running.GetValueOrDefault(inc) + (t.IncomeAmount ?? 0);
                running[inc] = balance;
                t.IncomeBalance = balance;
            }

            if (t.ExpenseAccount is { } exp)
            {
                var balance = running.GetValueOrDefault(exp) + (t.ExpenseAmount ?? 0);
                running[exp] = balance;
                t.ExpenseBalance = balance;
            }
        }

        await _db.SaveChangesAsync(ct);
    }
}
