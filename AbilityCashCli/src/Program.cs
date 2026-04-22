using System.Text;
using AbilityCashCli.Cli;
using AbilityCashCli.Configuration;
using AbilityCashCli.Data;
using AbilityCashCli.Import;
using AbilityCashCli.Import.BankStatements;
using AbilityCashCli.Import.BankStatements.Alfa;
using AbilityCashCli.Import.BankStatements.TBank;
using AbilityCashCli.Import.Handlers;
using AbilityCashCli.Import.SalaryRegisters;
using AbilityCashCli.Import.Timesheets;
using AbilityCashCli.Import.Vacations;
using Microsoft.EntityFrameworkCore;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

const string ArchiveFolderName = "imported";

try
{
    var cli = CliArgsParser.Parse(args);
    IConfigStore store = new ConfigStore();
    var config = cli.ApplyTo(store.Load());

    Console.WriteLine($"Config: {store.ConfigPath}");
    Console.WriteLine($"DB:     {config.DbPath}");

    if (cli.ImportPath is not null)
        return await RunImportAsync(config, [cli.ImportPath]);

    if (!string.IsNullOrWhiteSpace(config.ImportDir))
    {
        var scanner = new FolderImportScanner(ArchiveFolderName);
        var files = scanner.Enumerate(config.ImportDir).ToList();
        Console.WriteLine($"Scan:   {config.ImportDir} ({files.Count} файлов)");
        return await RunImportAsync(config, files);
    }

    return 0;
}
catch (ArgumentException ex)
{
    WriteError($"Ошибка: {ex.Message}");
    WriteError("Использование: AbilityCashCli [--db <path>] [--import <file>] [--import-dir <dir>]");
    return 1;
}
catch (Exception ex)
{
    WriteError($"Ошибка: {ex.Message}");
    return 2;
}

static void WriteError(string message)
{
    var prev = Console.ForegroundColor;
    Console.ForegroundColor = ConsoleColor.Red;
    try
    {
        Console.Error.WriteLine(message);
    }
    finally
    {
        Console.ForegroundColor = prev;
    }
}

static async Task<int> RunImportAsync(AppConfig config, IReadOnlyList<string> files)
{
    if (files.Count == 0)
    {
        Console.WriteLine("Нет файлов для импорта.");
        return 0;
    }

    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite($"Data Source={config.DbPath}")
        .Options;

    await using var db = new AppDbContext(options);

    var nameNormalizer = new PersonNameNormalizer(config.PersonAliases);

    var cashImporter = new CashPayoutsImporter(nameNormalizer);
    var cashHandler = new CashPayoutsHandler(
        cashImporter,
        new CashPayoutsWriter(db, config.CashPayouts, cashImporter.GetType()));

    var vacResolver = new CategoryPathResolver(db, config.Vacation.CategoryPathSeparator);
    var vacImporter = new VacationsImporter(nameNormalizer);
    var vacHandler = new VacationsHandler(
        vacImporter,
        new VacationsWriter(db, config.Vacation, vacResolver, Console.Out, vacImporter.GetType()));

    var tbankImporter = new TBankSalaryRegisterImporter(nameNormalizer);
    var tbankHandler = new TBankSalaryRegisterHandler(
        tbankImporter,
        new SalaryRegisterWriter(db, config.Enterprises, config.SalaryRegisters, tbankImporter.GetType()));

    var bankAccountResolver = new BankAccountResolver(db);
    var bankWriter = new BankStatementWriter(db);

    var alfaImporter = new AlfaBankImporter();
    var alfaHandler = new AlfaBankHandler(alfaImporter, bankAccountResolver, bankWriter);

    var tbankStatementImporter = new TBankStatementImporter();
    var tbankStatementHandler = new TBankStatementHandler(tbankStatementImporter, bankAccountResolver, bankWriter);

    var timesheetImporter = new TimesheetImporter(nameNormalizer);
    var timesheetHandler = new TimesheetHandler(
        timesheetImporter,
        new TimesheetWriter(db, config.Timesheet, config.Salaries, vacResolver, timesheetImporter.GetType()));

    IImportRouter router = new ImportRouter(new IImportHandler[] { cashHandler, vacHandler, tbankHandler, tbankStatementHandler, alfaHandler, timesheetHandler });
    IImportArchiver archiver = new FolderImportArchiver(Path.Combine(AppContext.BaseDirectory, ArchiveFolderName));

    var report = new ImportReportWriter(Console.Out, useConsoleColors: true);
    var balances = new AccountBalanceRecalculator(db);
    var runner = new BulkImportRunner(db, router, archiver, report, balances);
    await runner.RunAsync(files);
    return 0;
}
