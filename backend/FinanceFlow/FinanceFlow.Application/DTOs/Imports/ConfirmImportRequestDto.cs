namespace FinanceFlow.Application.DTOs.Imports;

public record ConfirmImportRequestDto(
    IEnumerable<ConfirmImportItemDto> Transactions
);

public record ConfirmImportItemDto(
    Guid Id,
    bool IsSelected,
    Guid CategoryId,
    string Type
);
