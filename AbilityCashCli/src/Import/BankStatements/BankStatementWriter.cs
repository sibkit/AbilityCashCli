using System.Text.RegularExpressions;
using AbilityCashCli.Configuration;
using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbilityCashCli.Import.BankStatements;

public sealed class BankStatementWriter
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly BankStatementsConfig _cfg;
    private readonly Type _importerType;

    public BankStatementWriter(AppDbContext db, BankStatementsConfig cfg, Type importerType)
    {
        _db = db;
        _cfg = cfg;
        _importerType = importerType;
    }

    public async Task<WriterResult> WriteAsync(string source, IReadOnlyList<AlfaBankRecord> records, CancellationToken ct = default)
    {
        if (records.Count == 0) return new WriterResult(0, Array.Empty<ImportError>());

        var errors = new List<ImportError>();

        var rch = records[0].Rch;
        var map = _cfg.AccountByRch;
        if (map is null || !map.TryGetValue(rch, out var accountName) || string.IsNullOrWhiteSpace(accountName))
        {
            errors.Add(new ImportError(source, null, "resolve",
                $"Нет маппинга для Rch '{rch}' в BankStatements.AccountByRch."));
            return new WriterResult(0, errors);
        }

        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Name == accountName && !a.Deleted, ct);
        if (account is null)
        {
            errors.Add(new ImportError(source, null, "resolve", $"Счёт '{accountName}' не найден."));
            return new WriterResult(0, errors);
        }

        var nowUnix = AbilityCashValues.NowUnix();
        var holderUnix = records.Min(r => AbilityCashValues.StartOfDayUnix(r.Date));
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

        var extra2 = AbilityCashValues.BuildSourceComment(source, _importerType);

        foreach (var r in records)
        {
            var stored = AbilityCashValues.ToStoredAmount(Math.Abs(r.AmountRur));
            var budgetDate = AbilityCashValues.StartOfDayUnix(r.Date);
            var counterparty = Normalize(r.CounterpartyName);
            var text70 = Normalize(r.Text70);
            var comment = $"[{counterparty}] {text70}";
            var extra1 = $"№{r.Number} от {r.ODate.ToString("dd.MM.yyyy")}, ИНН {r.CounterpartyInn}";

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
                Comment = comment,
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

        _db.TransactionGroups.Add(group);
        var saved = await _db.SaveChangesAsync(ct);
        return new WriterResult(saved, errors);
    }

    private static string Normalize(string value) =>
        WhitespaceRegex.Replace(value.Trim(), " ");
}
