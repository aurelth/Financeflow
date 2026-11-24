using FinanceFlow.Domain.Enums;
namespace FinanceFlow.Domain.Entities;

public class Transaction : BaseEntity
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; } = TransactionStatus.Paid;
    public bool IsRecurring { get; set; } = false;
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public Guid? RecurrenceGroupId { get; set; }

    // Compra parcelada: todas as parcelas compartilham o grupo; o total de parcelas fica em cada uma
    // para a tela mostrar "3/12" sem consultar o grupo. Nulos quando o lançamento não é parcelado.
    public Guid? InstallmentGroupId { get; set; }
    public int? InstallmentNumber { get; set; }
    public int? InstallmentCount { get; set; }
    public string? AttachmentPath { get; set; }
    public string? AttachmentName { get; set; }
    public string Tags { get; set; } = string.Empty; // JSON serializado
    public string? ImportHash { get; set; }
    public string? ExternalId { get; set; } // FITID do OFX de origem, quando importado

    /// <summary>Sentido da transferência (só para Type = Transfer). Define o efeito no saldo da conta.</summary>
    public TransferDirection? TransferDirection { get; set; }

    // Vínculo com a outra ponta da transferência (no máximo um dos dois existe)
    public TransferLink? LinkAsOut { get; set; }
    public TransferLink? LinkAsIn { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid? SubcategoryId { get; set; }
    public Subcategory? Subcategory { get; set; }
}
