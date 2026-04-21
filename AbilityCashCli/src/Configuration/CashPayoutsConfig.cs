namespace AbilityCashCli.Configuration;

public sealed record CashPayoutsConfig
{
    public string SalaryAccountPrefix { get; init; } = "";

    public string DefaultTime { get; init; } = "13:00";

    public IReadOnlyList<CashPayoutRoute> Routes { get; init; } = Array.Empty<CashPayoutRoute>();
}

public sealed record CashPayoutRoute
{
    public string Comment { get; init; } = "";

    public string SourceAccount { get; init; } = "";
}
