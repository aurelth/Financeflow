using CsvHelper.Configuration.Attributes;

namespace FinanceFlow.Workers.Models;

public class ReportCsvRow
{
    [Name("Data")]
    public string Date { get; set; } = string.Empty;

    [Name("Descrição")]
    public string Description { get; set; } = string.Empty;

    [Name("Tipo")]
    public string Type { get; set; } = string.Empty;

    [Name("Categoria")]
    public string CategoryName { get; set; } = string.Empty;

    [Name("Status")]
    public string Status { get; set; } = string.Empty;

    [Name("Valor (R$)")]
    public decimal Amount { get; set; }

    [Name("Tags")]
    public string Tags { get; set; } = string.Empty;
}
