using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs.Imports;

public record BankImportDto(
    Guid Id,
    string FileName,
    BankImportStatus Status,
    int TotalRecords,
    int Imported,
    int Duplicates,
    int Errors,
    string? ErrorMessage,
    DateTime CreatedAt
);
