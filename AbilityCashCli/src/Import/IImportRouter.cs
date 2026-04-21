namespace AbilityCashCli.Import;

public interface IImportRouter
{
    IImporter? Resolve(string path);
}
