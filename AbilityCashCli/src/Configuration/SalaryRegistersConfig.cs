namespace AbilityCashCli.Configuration;

public sealed record SalaryRegistersConfig
{
    public string SalaryAccountPrefix { get; init; } = "";

    public string DefaultTime { get; init; } = "14:00";
}
