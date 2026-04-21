namespace AbilityCashCli.Configuration;

public sealed record AppConfig
{
    public string DbPath { get; init; } = "";

    public string? ImportDir { get; init; }

    public CashPayoutsConfig CashPayouts { get; init; } = new();

    public VacationConfig Vacation { get; init; } = new();

    public IReadOnlyList<EnterpriseConfig> Enterprises { get; init; } = Array.Empty<EnterpriseConfig>();

    public SalaryRegistersConfig SalaryRegisters { get; init; } = new();

    public BankStatementsConfig BankStatements { get; init; } = new();

    public TimesheetConfig Timesheet { get; init; } = new();

    public IReadOnlyList<SalaryConfig> Salaries { get; init; } = Array.Empty<SalaryConfig>();

    public IReadOnlyList<PersonNameAliasConfig> PersonAliases { get; init; } = Array.Empty<PersonNameAliasConfig>();

    public static AppConfig CreateDefault() => new()
    {
        DbPath = Path.Combine(AppContext.BaseDirectory, "ability.db"),
        ImportDir = null,
        CashPayouts = new CashPayoutsConfig
        {
            SalaryAccountPrefix = "",
            DefaultTime = "13:00",
            Routes = Array.Empty<CashPayoutRoute>()
        },
        Vacation = new VacationConfig
        {
            SalaryAccountPrefix = "",
            SalaryCategoryPaths = Array.Empty<string>(),
            VacationCategoryPath = "",
            AverageDaysPerMonth = 29.3m,
            PreDaysOffset = 3,
            CategoryPathSeparator = "::"
        },
        Enterprises = Array.Empty<EnterpriseConfig>(),
        SalaryRegisters = new SalaryRegistersConfig
        {
            SalaryAccountPrefix = "",
            DefaultTime = "14:00"
        },
        BankStatements = new BankStatementsConfig
        {
            AccountByRch = new Dictionary<string, string>()
        },
        Timesheet = new TimesheetConfig
        {
            SalaryAccountPrefix = "",
            SalaryCategoryPath = "",
            DefaultTime = "12:00"
        },
        Salaries = Array.Empty<SalaryConfig>(),
        PersonAliases = Array.Empty<PersonNameAliasConfig>()
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
