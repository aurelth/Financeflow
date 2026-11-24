namespace FinanceFlow.Domain.ValueObjects;

public record OFXParseResult(
    string AccountId,
    string BankId,
    DateTime StartDate,
    DateTime EndDate,
    IEnumerable<OFXTransaction> Transactions,
    decimal? LedgerBalance = null,
    DateTime? LedgerBalanceDate = null
);

public record OFXTransaction(
    string FitId,
    DateTime Date,
    decimal Amount,
    string Description,
    string Type
);
