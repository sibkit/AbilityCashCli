namespace AbilityCashCli.Import;

public interface IImportRule
{
    IImporter Importer { get; }
    IImportWriter Writer { get; }
    bool Matches(string path);
}
