namespace AbilityCashCli.Import;

public interface IImportRule
{
    bool Matches(string path);

    Task<RuleResult> ExecuteAsync(string source, string path, CancellationToken ct = default);
}
