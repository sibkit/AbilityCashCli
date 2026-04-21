using AbilityCashCli.Import;

namespace AbilityCashCli.Cli;

public sealed class ImportReportWriter
{
    private readonly TextWriter _out;
    private readonly bool _useConsoleColors;

    public ImportReportWriter(TextWriter @out, bool useConsoleColors)
    {
        _out = @out;
        _useConsoleColors = useConsoleColors;
    }

    public void Ok(string path, int rows, int saved) =>
        WriteColored(ConsoleColor.Green, $"ok: {path} (rows={rows}, saved={saved})");

    public void Info(string message) =>
        _out.WriteLine(message);

    public void FileErrors(string path, IReadOnlyList<ImportError> errors)
    {
        WriteColored(ConsoleColor.Red, $"error: {path}");
        foreach (var e in errors)
        {
            var prefix = e.Row is null ? "[file]" : $"[row {e.Row}]";
            WriteColored(ConsoleColor.Red, $"  {prefix} {e.Phase}: {e.Message}");
        }
        WriteColored(ConsoleColor.Red, "---");
    }

    public void RollbackNotice() =>
        WriteColored(ConsoleColor.Red, "Есть ошибки — транзакция откатывается, архивация пропущена.");

    public void ErrorLine(string message) =>
        WriteColored(ConsoleColor.Red, message);

    public void BlankLine() => _out.WriteLine();

    private void WriteColored(ConsoleColor color, string message)
    {
        if (!_useConsoleColors)
        {
            _out.WriteLine(message);
            return;
        }

        var prev = Console.ForegroundColor;
        Console.ForegroundColor = color;
        try
        {
            _out.WriteLine(message);
        }
        finally
        {
            Console.ForegroundColor = prev;
        }
    }
}
