namespace AbilityCashCli.Import.BankStatements.TBank;

public sealed record TBankStatementRecord(
    DateTime Date,
    string DC,
    decimal AmountRur,
    string Number,
    string Account,
    string Description,
    string Purpose,
    string CounterpartyName,
    string CounterpartyInn,
    string CounterpartyAcc);
