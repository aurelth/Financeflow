namespace FinanceFlow.Application.DTOs.Imports;

public record ConfirmImportRequestDto(
    IEnumerable<ConfirmImportItemDto> Transactions
);

/// <param name="ImportAnyway">
/// Só vale para linhas marcadas como duplicadas: o usuário revisou e quer importar mesmo assim.
/// </param>
public record ConfirmImportItemDto(
    Guid Id,
    bool IsSelected,
    Guid CategoryId,
    string Type,
    bool ImportAnyway = false
);

/// <summary>Códigos de erro por linha devolvidos quando a confirmação é bloqueada.</summary>
public static class ImportRowErrors
{
    public const string MissingCategory = "MissingCategory";
    public const string InvalidCategory = "InvalidCategory";
}
