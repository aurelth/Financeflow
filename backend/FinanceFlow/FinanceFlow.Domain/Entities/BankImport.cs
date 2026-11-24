using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Domain.Entities;

public class BankImport : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public BankImportStatus Status { get; set; } = BankImportStatus.Pending;
    public int TotalRecords { get; set; } = 0;
    public int Imported { get; set; } = 0;
    public int Duplicates { get; set; } = 0;
    public int Errors { get; set; } = 0;
    public string? ErrorMessage { get; set; }

    // Identificação da conta bancária lida do arquivo (BANKID/ACCTID já em hash).
    // Permite aprender o vínculo ao trocar a conta da importação no preview.
    public string? BankId { get; set; }
    public string? AccountKeyHash { get; set; }
    public string? AccountLast4 { get; set; }

    // Saldo informado pelo banco no extrato (LEDGERBAL) e período coberto; base da conciliação
    public decimal? LedgerBalance { get; set; }
    public DateTime? LedgerBalanceDate { get; set; }
    public DateTime? StatementStart { get; set; }
    public DateTime? StatementEnd { get; set; }

    /// <summary>
    /// Quando o usuário confirmou a importação. Só extratos confirmados entram na conciliação,
    /// pois antes disso os lançamentos ainda não existem na conta.
    /// </summary>
    public DateTime? ConfirmedAt { get; set; }

    // Navegação
    public ICollection<BankImportTransaction> Transactions { get; set; } = [];
}
