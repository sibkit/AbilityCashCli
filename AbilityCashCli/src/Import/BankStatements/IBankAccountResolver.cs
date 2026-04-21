using AbilityCashCli.Data.Entities;

namespace AbilityCashCli.Import.BankStatements;

public interface IBankAccountResolver
{
    Task<Account> ResolveAsync(string fullAccountNumber, CancellationToken ct = default);
}
