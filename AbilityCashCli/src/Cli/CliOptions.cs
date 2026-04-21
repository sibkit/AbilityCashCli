using AbilityCashCli.Configuration;

namespace AbilityCashCli.Cli;

public sealed record CliOptions
{
    public string? DbPath { get; init; }
    public string? ImportPath { get; init; }
    public string? ImportDir { get; init; }

    public AppConfig ApplyTo(AppConfig config) => config with
    {
        DbPath = DbPath ?? config.DbPath,
        ImportDir = ImportDir ?? config.ImportDir
    };
}
