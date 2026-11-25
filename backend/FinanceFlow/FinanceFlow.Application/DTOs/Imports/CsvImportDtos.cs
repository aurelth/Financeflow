using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs.Imports;

/// <summary>Formatos de data aceitos na importação de CSV.</summary>
public static class CsvDateFormats
{
    public const string DayMonthYear = "dd/MM/yyyy";
    public const string DayMonthShortYear = "dd/MM/yy";
    public const string DayMonthYearDash = "dd-MM-yyyy";
    public const string DayMonthYearDot = "dd.MM.yyyy";
    public const string IsoDate = "yyyy-MM-dd";
    public const string MonthDayYear = "MM/dd/yyyy";

    public static readonly IReadOnlyList<string> All =
    [
        DayMonthYear, DayMonthShortYear, DayMonthYearDash, DayMonthYearDot, IsoDate, MonthDayYear,
    ];
}

/// <summary>
/// Como ler as colunas de um CSV de extrato. Os índices começam em 0.
/// O valor vem em uma coluna com sinal (<see cref="AmountColumn"/>) ou separado em débito e crédito.
/// </summary>
public record CsvMappingDto(
    string Delimiter,
    bool HasHeader,
    int DateColumn,
    int DescriptionColumn,
    int? AmountColumn,
    int? DebitColumn,
    int? CreditColumn,
    string DateFormat,
    string DecimalSeparator,
    // Faturas de cartão listam as compras como valores positivos: inverte o sinal lido
    bool InvertSign = false
);

/// <summary>Motivos pelos quais uma linha do CSV não vira transação.</summary>
public static class CsvRowErrors
{
    public const string InvalidDate = "InvalidDate";
    public const string InvalidAmount = "InvalidAmount";
    public const string ZeroAmount = "ZeroAmount";
    public const string MissingColumns = "MissingColumns";
}

public record CsvRowErrorDto(int Line, string Code);

public record CsvPreviewRowDto(
    int Line,
    DateTime Date,
    decimal Amount,
    string Description,
    TransactionType Type
);

/// <summary>
/// Resultado da leitura do arquivo: como ele foi decodificado, as primeiras linhas cruas
/// (para o usuário mapear as colunas) e o que o mapeamento aplicado produz.
/// </summary>
public record CsvAnalysisDto(
    string Encoding,
    int Columns,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> SampleRows,
    int TotalRows,
    CsvMappingDto Mapping,
    int ValidRows,
    int InvalidRows,
    IReadOnlyList<CsvPreviewRowDto> Preview,
    IReadOnlyList<CsvRowErrorDto> Errors
);
