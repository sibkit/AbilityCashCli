namespace AbilityCashCli.Configuration;

public sealed record EnterpriseConfig
{
    public string Name { get; init; } = "";

    public string PayrollAccount { get; init; } = "";
}
