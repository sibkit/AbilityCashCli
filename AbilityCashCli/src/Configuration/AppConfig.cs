namespace AbilityCashCli.Configuration;

public sealed record AppConfig
{
    public string DbPath { get; init; } = "";

    public string ImportAccountName { get; init; } = "";

    public string? ImportDir { get; init; }

    public static AppConfig CreateDefault() => new()
    {
        DbPath = Path.Combine(AppContext.BaseDirectory, "ability.db"),
        ImportAccountName = "",
        ImportDir = null
    };
}
