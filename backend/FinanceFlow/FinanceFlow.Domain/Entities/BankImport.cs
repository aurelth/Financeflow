using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Domain.Entities;

public class BankImport : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public BankImportStatus Status { get; set; } = BankImportStatus.Pending;
    public int TotalRecords { get; set; } = 0;
    public int Imported { get; set; } = 0;
    public int Duplicates { get; set; } = 0;
    public int Errors { get; set; } = 0;
    public string? ErrorMessage { get; set; }

    // Navegação
    public ICollection<BankImportTransaction> Transactions { get; set; } = [];
}
