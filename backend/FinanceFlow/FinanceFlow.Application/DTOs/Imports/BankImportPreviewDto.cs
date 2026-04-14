namespace FinanceFlow.Application.DTOs.Imports;

public record BankImportPreviewDto(
    Guid ImportId,
    string FileName,
    int TotalRecords,
    int Duplicates,
    IEnumerable<BankImportTransactionDto> Transactions
);
