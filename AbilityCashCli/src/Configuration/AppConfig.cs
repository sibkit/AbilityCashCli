namespace AbilityCashCli.Configuration;

public sealed record AppConfig
{
    public string DbPath { get; init; } = "";

    public string ImportAccountName { get; init; } = "";

    public string? ImportDir { get; init; }

    public VacationConfig Vacation { get; init; } = new();

    public static AppConfig CreateDefault() => new()
    {
        DbPath = Path.Combine(AppContext.BaseDirectory, "ability.db"),
        ImportAccountName = "",
        ImportDir = null,
        Vacation = new VacationConfig
        {
            SalaryAccountPrefix = "",
            SalaryCategoryPaths = Array.Empty<string>(),
            VacationCategoryPath = "",
            AverageDaysPerMonth = 29.3m,
            PreDaysOffset = 3,
            CategoryPathSeparator = "::"
        }
    };
}

public sealed record VacationConfig
{
    public string SalaryAccountPrefix { get; init; } = "";

    public IReadOnlyList<string> SalaryCategoryPaths { get; init; } = Array.Empty<string>();

    public string VacationCategoryPath { get; init; } = "";

    public decimal AverageDaysPerMonth { get; init; } = 29.3m;

    public int PreDaysOffset { get; init; } = 3;

    public string CategoryPathSeparator { get; init; } = "::";
}
