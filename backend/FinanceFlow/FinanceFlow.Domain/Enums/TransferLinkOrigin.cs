namespace FinanceFlow.Domain.Enums;

/// <summary>Como o vínculo entre as duas pontas de uma transferência foi criado.</summary>
public enum TransferLinkOrigin
{
    /// <summary>Pareamento automático na importação (pontuação alta).</summary>
    Auto = 1,

    /// <summary>Sugestão de pareamento aceita pelo usuário.</summary>
    Suggested = 2,

    /// <summary>Criado pelo usuário (transferência manual ou vínculo de lançamentos existentes).</summary>
    Manual = 3,
}
