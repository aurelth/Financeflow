namespace FinanceFlow.Application.Services;

/// <summary>
/// Regras de uma compra parcelada: como o total vira parcelas e em que datas elas vencem.
/// Funções puras (sem I/O) para serem testadas em isolamento.
/// </summary>
public static class InstallmentPlanner
{
    public const int MinInstallments = 2;
    public const int MaxInstallments = 60;

    /// <summary>
    /// Divide o total em <paramref name="count"/> parcelas em centavos, sem perder nem criar dinheiro:
    /// a soma das parcelas é sempre exatamente o total. Os centavos que sobram da divisão vão um a um
    /// para as primeiras parcelas (100,00 em 3x = 33,34 + 33,33 + 33,33), como faz o cartão.
    /// </summary>
    public static IReadOnlyList<decimal> Split(decimal total, int count)
    {
        if (count < 1)
            throw new ArgumentOutOfRangeException(nameof(count), "O número de parcelas deve ser positivo.");

        var totalCents = (long)Math.Round(total * 100m, 0, MidpointRounding.AwayFromZero);
        var baseCents = totalCents / count;
        var extraCents = (int)(totalCents % count);

        var parcels = new decimal[count];
        for (var i = 0; i < count; i++)
            parcels[i] = (baseCents + (i < extraCents ? 1 : 0)) / 100m;

        return parcels;
    }

    /// <summary>
    /// Vencimento da parcela <paramref name="number"/> (1 = primeira). Conta a partir da data da primeira,
    /// não da parcela anterior, para o dia não "escorregar": 31/01 → 28/02 → 31/03 (e não 28/03).
    /// </summary>
    public static DateTime DueDate(DateTime firstDate, int number) =>
        firstDate.AddMonths(number - 1);
}
