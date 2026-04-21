namespace AbilityCashCli.Configuration;

public sealed record TimesheetConfig
{
    public string SalaryAccountPrefix { get; init; } = "";

    public string SalaryCategoryPath { get; init; } = "";
}
