namespace AbilityCashCli.Cli;

public static class CliArgsParser
{
    public static CliOptions Parse(string[] args)
    {
        string? dbPath = null;
        string? importPath = null;
        string? importDir = null;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--db":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("--db требует значение");
                    dbPath = args[++i];
                    break;

                case "--import":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("--import требует путь к файлу");
                    importPath = args[++i];
                    break;

                case "--import-dir":
                    if (i + 1 >= args.Length)
                        throw new ArgumentException("--import-dir требует путь к каталогу");
                    importDir = args[++i];
                    break;

                default:
                    throw new ArgumentException($"Неизвестный аргумент: {args[i]}");
            }
        }

        return new CliOptions { DbPath = dbPath, ImportPath = importPath, ImportDir = importDir };
    }
}
