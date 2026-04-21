namespace AbilityCashCli.Import;

public interface IImportScanner
{
    IEnumerable<string> Enumerate(string dir);
}
