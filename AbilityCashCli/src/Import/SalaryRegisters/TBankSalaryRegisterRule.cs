namespace AbilityCashCli.Import.SalaryRegisters;

public sealed class TBankSalaryRegisterRule : IImportRule
{
    public TBankSalaryRegisterRule(IImporter importer, IImportWriter writer)
    {
        Importer = importer;
        Writer = writer;
    }

    public IImporter Importer { get; }

    public IImportWriter Writer { get; }

    public bool Matches(string path)
    {
        if (!string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase))
            return false;
        return TBankSalaryRegisterImporter.HasHeader(path);
    }
}
