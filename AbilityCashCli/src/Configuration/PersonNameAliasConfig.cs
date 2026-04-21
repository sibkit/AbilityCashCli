namespace AbilityCashCli.Configuration;

public sealed record PersonNameAliasConfig
{
    public string From { get; init; } = "";

    public string To { get; init; } = "";
}
