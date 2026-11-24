namespace FinanceFlow.Domain.Enums;

/// <summary>Confiança do pareamento de uma linha do extrato com um lançamento de outra conta.</summary>
public enum TransferMatchStatus
{
    /// <summary>Pontuação alta: vira transferência automaticamente (o usuário pode desfazer no preview).</summary>
    Auto = 1,

    /// <summary>Pontuação média: só vira transferência se o usuário aceitar a sugestão.</summary>
    Suggested = 2,
}
