namespace FinanceFlow.Application.DTOs.Imports;

public record ConfirmImportRequestDto(
    IEnumerable<ConfirmImportItemDto> Transactions
);

/// <param name="ImportAnyway">
/// Só vale para linhas marcadas como duplicadas: o usuário revisou e quer importar mesmo assim.
/// </param>
/// <param name="TransferAction">
/// Só vale para linhas com par de transferência: <see cref="TransferActions.Link"/> vincula as duas pontas,
/// <see cref="TransferActions.Ignore"/> importa como lançamento comum e lembra que o par foi rejeitado.
/// Sem valor: pares automáticos são vinculados e sugestões são ignoradas (sem rejeitar).
/// </param>
public record ConfirmImportItemDto(
    Guid Id,
    bool IsSelected,
    Guid CategoryId,
    string Type,
    bool ImportAnyway = false,
    string? TransferAction = null
);

public static class TransferActions
{
    public const string Link = "Link";
    public const string Ignore = "Ignore";
}

/// <summary>Códigos de erro por linha devolvidos quando a confirmação é bloqueada.</summary>
public static class ImportRowErrors
{
    public const string MissingCategory = "MissingCategory";
    public const string InvalidCategory = "InvalidCategory";
    public const string UnknownTransferDirection = "UnknownTransferDirection";
}
