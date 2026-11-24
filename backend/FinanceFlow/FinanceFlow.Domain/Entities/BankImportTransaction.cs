using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Domain.Entities;

public class BankImportTransaction : BaseEntity
{
    public Guid BankImportId { get; set; }
    public BankImport BankImport { get; set; } = null!;

    // Dados extraídos do OFX
    public string ExternalId { get; set; } = string.Empty; // FITID do OFX
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionType Type { get; set; }
    public string Hash { get; set; } = string.Empty; // hash para deduplicação

    // Categorização sugerida
    public Guid? SuggestedCategoryId { get; set; }
    public Category? SuggestedCategory { get; set; }
    public CategorySuggestionSource? SuggestionSource { get; set; }

    // Status da transação no preview
    public bool IsDuplicate { get; set; } = false;
    public bool IsSelected { get; set; } = true;  // selecionada para importar
    public DuplicateReason? DuplicateReason { get; set; }
    public Guid? MatchedTransactionId { get; set; } // lançamento existente que originou a suspeita

    // Pareamento com a outra ponta de uma transferência (lançamento de outra conta)
    public Guid? TransferMatchTransactionId { get; set; }
    public int? TransferMatchScore { get; set; }
    public TransferMatchStatus? TransferMatchStatus { get; set; }

    // Transação criada após confirmação
    public Guid? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }
}
