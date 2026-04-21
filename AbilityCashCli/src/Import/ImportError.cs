namespace AbilityCashCli.Import;

public sealed record ImportError(string File, int? Row, string Phase, string Message);

public sealed record HandlerResult(int RowsRead, int RowsSaved, IReadOnlyList<ImportError> Errors);

public sealed record WriterResult(int Saved, IReadOnlyList<ImportError> Errors);
