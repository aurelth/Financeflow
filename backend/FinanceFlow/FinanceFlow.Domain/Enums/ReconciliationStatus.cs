namespace FinanceFlow.Domain.Enums;

/// <summary>Resultado da comparação entre o saldo do extrato e o saldo calculado da conta.</summary>
public enum ReconciliationStatus
{
    /// <summary>Nenhuma importação confirmada trouxe o saldo do extrato (LEDGERBAL).</summary>
    NoStatement = 1,

    /// <summary>Há extrato, mas não dá para comparar (ex.: o saldo inicial da conta é posterior à data do extrato).</summary>
    Unavailable = 2,

    /// <summary>O saldo calculado na data do extrato é igual ao saldo informado pelo banco.</summary>
    Matched = 3,

    /// <summary>O saldo calculado na data do extrato difere do saldo informado pelo banco.</summary>
    Divergent = 4,
}
