namespace FinanceFlow.Domain.Enums;

/// <summary>Sentido de uma ponta de transferência, do ponto de vista da conta onde ela foi lançada.</summary>
public enum TransferDirection
{
    /// <summary>Dinheiro saiu da conta.</summary>
    Out = 1,

    /// <summary>Dinheiro entrou na conta.</summary>
    In = 2,
}
