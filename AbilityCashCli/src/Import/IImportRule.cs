namespace AbilityCashCli.Import;

public interface IImportRule
{
    IImporter Importer { get; }
    bool Matches(string path);
}
