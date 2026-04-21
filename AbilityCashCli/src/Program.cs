using System.Text;
using AbilityCashCli.Cli;
using AbilityCashCli.Configuration;
using AbilityCashCli.Data;
using AbilityCashCli.Import;
using AbilityCashCli.Import.Rules;
using AbilityCashCli.Import.SalaryRegisters;
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
    Console.Error.WriteLine($"Ошибка: {ex.Message}");
    Console.Error.WriteLine("Использование: AbilityCashCli [--db <path>] [--import <file>] [--import-dir <dir>]");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Ошибка: {ex.Message}");
    return 2;
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

    var cashImporter = new CashPayoutsImporter();
    var cashRule = new CashPayoutsRule(
        cashImporter,
        new CashPayoutsWriter(db, config.CashPayouts, cashImporter.GetType()));

    var vacResolver = new CategoryPathResolver(db, config.Vacation.CategoryPathSeparator);
    var vacImporter = new VacationsImporter();
    var vacRule = new VacationsRule(
        vacImporter,
        new VacationsWriter(db, config.Vacation, vacResolver, Console.Out, vacImporter.GetType()));

    var tbankImporter = new TBankSalaryRegisterImporter();
    var tbankRule = new TBankSalaryRegisterRule(
        tbankImporter,
        new SalaryRegisterWriter(db, config.Enterprises, config.SalaryRegisters, tbankImporter.GetType()));

    IImportRouter router = new ImportRouter(new IImportRule[] { cashRule, vacRule, tbankRule });
    IImportArchiver archiver = new FolderImportArchiver(Path.Combine(AppContext.BaseDirectory, ArchiveFolderName));

    var runner = new BulkImportRunner(router, archiver, Console.Out);
    await runner.RunAsync(files);
    return 0;
}
