using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbilityCashCli.Import.BankStatements;

public sealed class BankAccountResolver : IBankAccountResolver
{
    private readonly AppDbContext _db;

    public BankAccountResolver(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Account> ResolveAsync(string fullAccountNumber, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fullAccountNumber) || fullAccountNumber.Length < 4)
            throw new InvalidOperationException(
                $"Не удалось определить суффикс счёта из '{fullAccountNumber}'.");

        var suffix = fullAccountNumber[^4..];
        var marker = $"[..{suffix}]";

        var matches = await _db.Accounts
            .Where(a => !a.Deleted && a.Name.Contains(marker))
            .ToListAsync(ct);

        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException(
                $"Счёт с суффиксом '{marker}' не найден среди активных счетов."),
            _ => throw new InvalidOperationException(
                $"Найдено {matches.Count} счетов с суффиксом '{marker}': " +
                string.Join(", ", matches.Select(m => $"'{m.Name}'")) + ".")
        };
    }
}
