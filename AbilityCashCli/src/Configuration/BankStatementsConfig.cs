namespace AbilityCashCli.Configuration;

public sealed record BankStatementsConfig
{
    public IReadOnlyDictionary<string, string> AccountByRch { get; init; } =
        new Dictionary<string, string>();
}
