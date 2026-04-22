namespace AbilityCashCli.Import;

public sealed record ImportRecord(
    DateTime Date,
    decimal Amount,
    string Person,
    string Comment,
    string? EnterpriseHint = null);
