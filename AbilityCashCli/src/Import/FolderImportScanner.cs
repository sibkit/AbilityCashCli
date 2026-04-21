namespace AbilityCashCli.Import;

public sealed class FolderImportScanner : IImportScanner
{
    private readonly string _archiveFolderName;

    public FolderImportScanner(string archiveFolderName)
    {
        _archiveFolderName = archiveFolderName;
    }

    public IEnumerable<string> Enumerate(string dir)
    {
        if (!Directory.Exists(dir))
            yield break;

        foreach (var path in Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly))
        {
            var parent = Path.GetFileName(Path.GetDirectoryName(path));
            if (string.Equals(parent, _archiveFolderName, StringComparison.OrdinalIgnoreCase))
                continue;
            yield return path;
        }
    }
}
