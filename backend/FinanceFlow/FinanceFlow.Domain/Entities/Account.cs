using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Domain.Entities;

/// <summary>
/// Conta financeira do usuário (corrente, poupança, cartão, carteira, investimento).
/// Cada transação e cada importação pertencem a uma conta.
/// </summary>
public class Account : BaseEntity
{
    public const string DefaultName = "Conta principal";

    /// <summary>Cria a "Conta principal" de um usuário.</summary>
    public static Account CreateDefault(Guid userId, string? currency = null) => new()
    {
        UserId = userId,
        Name = DefaultName,
        IsDefault = true,
        Currency = string.IsNullOrWhiteSpace(currency) || currency.Length != 3 ? "BRL" : currency,
    };

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; } = AccountType.Checking;

    // Identificação bancária vinda do OFX (BANKID) e nome para exibição
    public string? BankId { get; set; }
    public string? BankName { get; set; }

    /// <summary>
    /// HMAC do ACCTID do OFX, usado apenas para reconhecer a conta em novas importações.
    /// O número completo da conta nunca é guardado.
    /// </summary>
    public string? AccountKeyHash { get; set; }

    /// <summary>Últimos 4 dígitos do ACCTID, apenas para exibição.</summary>
    public string? AccountLast4 { get; set; }

    // Ponto de partida do saldo calculado
    public decimal InitialBalance { get; set; }
    public DateTime? InitialBalanceDate { get; set; }

    public string Currency { get; set; } = "BRL";
    public string Color { get; set; } = "#10B981";
    public string Icon { get; set; } = "landmark";

    /// <summary>Conta criada automaticamente ("Conta principal"); não pode ser arquivada.</summary>
    public bool IsDefault { get; set; }

    /// <summary>Arquivar em vez de apagar, para não perder o histórico.</summary>
    public bool IsArchived { get; set; }
}
