using AbilityCashCli.Configuration;
using AbilityCashCli.Data;
using AbilityCashCli.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbilityCashCli.Import.SalaryRegisters;

public sealed class SalaryRegisterWriter : IImportWriter
{
    private readonly AppDbContext _db;
    private readonly IReadOnlyList<EnterpriseConfig> _enterprises;
    private readonly SalaryRegistersConfig _cfg;
    private readonly Type _importerType;

    public SalaryRegisterWriter(
        AppDbContext db,
        IReadOnlyList<EnterpriseConfig> enterprises,
        SalaryRegistersConfig cfg,
        Type importerType)
    {
        _db = db;
        _enterprises = enterprises;
        _cfg = cfg;
        _importerType = importerType;
    }

    public async Task<WriterResult> WriteAsync(string source, IReadOnlyList<ImportRecord> records, CancellationToken ct = default)
    {
        if (records.Count == 0) return new WriterResult(0, Array.Empty<ImportError>());

        var errors = new List<ImportError>();

        var enterprise = ResolveEnterprise(source, records[0].EnterpriseHint, out var enterpriseError);
        if (enterprise is null)
        {
            errors.Add(new ImportError(source, null, "resolve", enterpriseError!));
            return new WriterResult(0, errors);
        }

        var nowUnix = AbilityCashValues.NowUnix();

        var src = await _db.Accounts.FirstOrDefaultAsync(a => a.Name == enterprise.PayrollAccount && !a.Deleted, ct);
        if (src is null)
        {
            errors.Add(new ImportError(source, null, "resolve", $"Счёт источника '{enterprise.PayrollAccount}' не найден."));
            return new WriterResult(0, errors);
        }

        var resolved = new List<(ImportRecord Record, Account Dst, int BudgetDate)>();
        var dstCache = new Dictionary<string, Account>(StringComparer.Ordinal);

        for (var i = 0; i < records.Count; i++)
        {
            var r = records[i];
            var row = i + 1;
            var dstName = _cfg.SalaryAccountPrefix + r.Person;
            if (!dstCache.TryGetValue(dstName, out var dst))
            {
                dst = await _db.Accounts.FirstOrDefaultAsync(a => a.Name == dstName && !a.Deleted, ct);
                if (dst is null)
                {
                    errors.Add(new ImportError(source, row, "resolve", $"Счёт назначения '{dstName}' не найден."));
                    continue;
                }
                dstCache[dstName] = dst;
            }

            var budgetDate = AbilityCashValues.StartOfDayUnix(r.Date);
            resolved.Add((r, dst, budgetDate));
        }

        if (resolved.Count == 0)
            return new WriterResult(0, errors);

        var groups = new TransactionGroupAllocator(_db, nowUnix);
        var extra = AbilityCashValues.BuildSourceComment(source, _importerType);

        foreach (var (r, dst, budgetDate) in resolved)
        {
            var stored = AbilityCashValues.ToStoredAmount(Math.Abs(r.Amount));
            var group = await groups.NewGroupAsync(budgetDate, ct);
            group.Transactions.Add(new Transaction
            {
                Guid = AbilityCashValues.NewGuidBytes(),
                Changed = nowUnix,
                Deleted = 0,
                Position = 0,
                BudgetDate = budgetDate,
                Executed = 1,
                Locked = 0,
                ExpenseAccount = src.Id,
                ExpenseAmount = -stored,
                IncomeAccount = dst.Id,
                IncomeAmount = stored,
                Quantity = AbilityCashValues.QuantityOne,
                Comment = r.Comment,
                ExtraComment1 = "",
                ExtraComment2 = extra,
                ExtraComment3 = "",
                ExtraComment4 = "",
                BudgetPeriodEnd = budgetDate + AbilityCashValues.DaySeconds
            });
        }

        var saved = await _db.SaveChangesAsync(ct);
        return new WriterResult(saved, errors);
    }

    private EnterpriseConfig? ResolveEnterprise(string source, string? hint, out string? error)
    {
        error = null;
        var hasHint = !string.IsNullOrWhiteSpace(hint);
        var matches = _enterprises
            .Where(e => !string.IsNullOrEmpty(e.Name)
                        && (hasHint
                            ? e.Name.Contains(hint!, StringComparison.OrdinalIgnoreCase)
                            : source.Contains(e.Name, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        if (matches.Count == 0)
        {
            error = hasHint
                ? $"По подсказке '{hint}' не найдено ни одного Enterprise.Name."
                : $"В имени '{source}' не найдено ни одного Enterprise.Name.";
            return null;
        }
        if (matches.Count > 1)
        {
            error = hasHint
                ? $"По подсказке '{hint}' найдено несколько Enterprise: {string.Join(", ", matches.Select(m => m.Name))}."
                : $"В имени '{source}' найдено несколько Enterprise: {string.Join(", ", matches.Select(m => m.Name))}.";
            return null;
        }
        return matches[0];
    }
}
