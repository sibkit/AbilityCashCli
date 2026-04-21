namespace AbilityCashCli.Import;

public interface IImportRouter
{
    IImportRule? Resolve(string path);
}
