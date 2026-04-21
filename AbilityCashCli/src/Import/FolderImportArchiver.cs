namespace AbilityCashCli.Import;

public sealed class FolderImportArchiver : IImportArchiver
{
    private readonly string _archiveDir;

    public FolderImportArchiver(string archiveDir)
    {
        _archiveDir = archiveDir;
    }

    public string Archive(string path)
    {
        Directory.CreateDirectory(_archiveDir);

        var name = Path.GetFileName(path);
        var target = Path.Combine(_archiveDir, name);
        if (File.Exists(target))
        {
            var stem = Path.GetFileNameWithoutExtension(name);
            var ext = Path.GetExtension(name);
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            target = Path.Combine(_archiveDir, $"{stem}_{stamp}{ext}");
        }

        File.Move(path, target);
        return target;
    }
}
