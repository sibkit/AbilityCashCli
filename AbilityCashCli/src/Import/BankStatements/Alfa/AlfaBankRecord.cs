namespace AbilityCashCli.Import.BankStatements.Alfa;

public sealed record AlfaBankRecord(
    DateTime Date,
    string DC,
    decimal AmountRur,
    string Number,
    DateTime ODate,
    string Text70,
    string Rch,
    string Currency,
    string CounterpartyName,
    string CounterpartyInn,
    string CounterpartyAcc);
