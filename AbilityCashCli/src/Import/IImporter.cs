namespace AbilityCashCli.Import;

public interface IImporter
{
    IReadOnlyList<ImportRecord> Read(string path);
}
