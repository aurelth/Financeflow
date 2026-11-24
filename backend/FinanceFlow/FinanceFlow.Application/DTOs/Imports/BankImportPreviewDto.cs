namespace FinanceFlow.Application.DTOs.Imports;

public record BankImportPreviewDto(
    Guid ImportId,
    string FileName,
    int TotalRecords,
    int Duplicates,
    IEnumerable<BankImportTransactionDto> Transactions,
    Guid? AccountId = null,
    string? AccountName = null,
    string? Status = null,
    int AutoTransfers = 0,
    int SuggestedTransfers = 0
);
