namespace AbilityCashCli.Import;

public sealed class ImportRouter : IImportRouter
{
    private readonly IReadOnlyList<IImportRule> _rules;

    public ImportRouter(IEnumerable<IImportRule> rules)
    {
        _rules = rules.ToList();
    }

    public IImportRule? Resolve(string path)
    {
        foreach (var rule in _rules)
            if (rule.Matches(path))
                return rule;
        return null;
    }
}
