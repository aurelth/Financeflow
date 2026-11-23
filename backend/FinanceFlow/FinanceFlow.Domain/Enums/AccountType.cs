namespace FinanceFlow.Domain.Enums;

/// <summary>Tipo de conta financeira do usuário.</summary>
public enum AccountType
{
    Checking = 1,    // Conta corrente
    Savings = 2,     // Poupança
    CreditCard = 3,  // Cartão de crédito
    Cash = 4,        // Carteira / dinheiro
    Investment = 5   // Investimento
}
