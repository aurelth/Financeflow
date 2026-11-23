using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs.Imports;

/// <summary>Como a conta da importação foi definida.</summary>
public static class AccountResolutions
{
    /// <summary>O usuário escolheu a conta.</summary>
    public const string Provided = "Provided";
    /// <summary>A conta foi reconhecida pelo ACCTID/BANKID do arquivo.</summary>
    public const string Recognized = "Recognized";
    /// <summary>O usuário escolheu a conta e o arquivo passou a identificá-la nas próximas importações.</summary>
    public const string Learned = "Learned";
    /// <summary>Nada identificou a conta: foi usada a "Conta principal".</summary>
    public const string Defaulted = "Defaulted";
}

public record BankImportDto(
    Guid Id,
    string FileName,
    BankImportStatus Status,
    int TotalRecords,
    int Imported,
    int Duplicates,
    int Errors,
    string? ErrorMessage,
    DateTime CreatedAt,
    Guid? AccountId = null,
    string? AccountName = null,
    string? AccountResolution = null
);
