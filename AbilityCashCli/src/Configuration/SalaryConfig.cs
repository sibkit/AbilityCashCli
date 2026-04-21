namespace AbilityCashCli.Configuration;

public sealed record SalaryConfig
{
    public string Person { get; init; } = "";

    public decimal Amount { get; init; }
}
