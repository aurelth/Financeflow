namespace FinanceFlow.Application.Common.Interfaces;

/// <summary>
/// Gera a chave (hash) usada para reconhecer uma conta bancária a partir do ACCTID do OFX,
/// sem guardar o número completo da conta.
/// </summary>
public interface IAccountKeyHasher
{
    string ComputeKey(Guid userId, string? bankId, string accountId);

    /// <summary>Últimos 4 caracteres do identificador, só para exibição.</summary>
    string Last4(string accountId);
}
