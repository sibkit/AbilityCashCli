namespace AbilityCashCli.Import.BankStatements;

public sealed record BankStatementRow(
    DateTime Date,
    string DC,
    decimal Amount,
    string Number,
    DateTime ODate,
    string Comment,
    string CounterpartyName,
    string CounterpartyInn,
    string CounterpartyAcc);
