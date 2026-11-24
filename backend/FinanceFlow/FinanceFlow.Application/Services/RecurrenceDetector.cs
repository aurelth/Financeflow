using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.Services;

/// <summary>Natureza da recorrência detectada, que decide como a tela a apresenta.</summary>
public enum RecurringKind
{
    /// <summary>Despesa de valor fixo (streaming, academia, mensalidade).</summary>
    Subscription = 1,

    /// <summary>Despesa que se repete com valor variável (luz, água, cartão).</summary>
    Bill = 2,

    /// <summary>Receita que se repete (salário, aluguel recebido).</summary>
    Income = 3,
}

/// <summary>Uma recorrência encontrada no histórico, com as ocorrências que a sustentam (da mais antiga à mais recente).</summary>
public sealed record DetectedRecurrence(
    string Key,
    TransactionType Type,
    RecurrenceType Cadence,
    RecurringKind Kind,
    decimal Amount,
    bool AmountVaries,
    DateTime FirstDate,
    DateTime LastDate,
    DateTime NextExpectedDate,
    decimal MonthlyCost,
    double Confidence,
    IReadOnlyList<Transaction> Occurrences)
{
    public Transaction Latest => Occurrences[^1];
}

/// <summary>
/// Encontra assinaturas e contas recorrentes no histórico: lançamentos de mesma descrição (normalizada) e mesmo tipo
/// que se repetem em intervalo regular — semanal, mensal ou anual — com valor estável.
/// É uma função pura do histórico: não grava nada, e por isso a sugestão nunca fica defasada em relação aos lançamentos.
/// </summary>
public static class RecurrenceDetector
{
    /// <summary>Quanto histórico a detecção olha. 25 meses para enxergar ao menos duas ocorrências de uma anual.</summary>
    public const int LookbackMonths = 25;

    // Mínimo de ocorrências para acreditar na regularidade: duas só provam um intervalo, não um padrão
    // (exceto a anual, em que duas ocorrências já são tudo o que o histórico alcança).
    private const int MinWeekly = 4;
    private const int MinMonthly = 3;
    private const int MinYearly = 2;

    // Parcela mínima dos intervalos que precisam cair na janela da cadência (tolera um mês pulado)
    private const double MinRegularShare = 0.75;

    // Até quanto o valor pode variar em relação à mediana: valor fixo (reajuste de plano) ou conta variável
    private const double FixedAmountTolerance = 0.05;
    private const double VariableAmountTolerance = 0.40;

    // Quanto a próxima ocorrência pode atrasar antes de a série ser tratada como encerrada (assinatura cancelada)
    private const int WeeklyGraceDays = 7;
    private const int MonthlyGraceDays = 15;
    private const int YearlyGraceDays = 30;

    // Quantas ocorrências futuras uma série confirmada projeta
    private const int WeeklyHorizon = 12;
    private const int MonthlyHorizon = 12;
    private const int YearlyHorizon = 2;

    /// <param name="transactions">Histórico do usuário (já sem transferências e parcelas).</param>
    /// <param name="dismissed">Dispensas do usuário: (tipo, chave) que não devem voltar a ser sugeridas.</param>
    /// <param name="today">Data de referência (UTC), para decidir se a série ainda está ativa.</param>
    public static IReadOnlyList<DetectedRecurrence> Detect(
        IEnumerable<Transaction> transactions,
        IReadOnlySet<(TransactionType Type, string Key)> dismissed,
        DateTime today)
    {
        var groups = transactions
            .Where(t => t.Type != TransactionType.Transfer)
            .Select(t => (Transaction: t, Key: DescriptionNormalizer.ToKey(t.Description)))
            .Where(x => x.Key.Length > 0)
            .GroupBy(x => (x.Transaction.Type, x.Key));

        var found = new List<DetectedRecurrence>();

        foreach (var group in groups)
        {
            // Já existe uma série recorrente com esta descrição: não sugerir de novo (nem duplicar a projeção)
            if (group.Any(x => x.Transaction.IsRecurring))
                continue;

            if (dismissed.Contains((group.Key.Type, group.Key.Key)))
                continue;

            var occurrences = group
                .Select(x => x.Transaction)
                .OrderBy(t => t.Date)
                .ToList();

            var detected = Analyze(group.Key.Type, group.Key.Key, occurrences, today.Date);
            if (detected is not null)
                found.Add(detected);
        }

        return found
            .OrderByDescending(d => d.Confidence)
            .ThenByDescending(d => d.MonthlyCost)
            .ToList();
    }

    /// <summary>Datas futuras de uma série confirmada: ancoradas na última ocorrência, só depois de <paramref name="today"/>.</summary>
    public static IReadOnlyList<DateTime> ProjectDates(DetectedRecurrence recurrence, DateTime today)
    {
        var horizon = recurrence.Cadence switch
        {
            RecurrenceType.Weekly => WeeklyHorizon,
            RecurrenceType.Yearly => YearlyHorizon,
            _ => MonthlyHorizon,
        };

        var anchor = recurrence.Latest.Date;
        var dates = new List<DateTime>(horizon);

        // Ancorar sempre na última ocorrência (e não encadear) mantém o dia: 31/jan → 28/fev → 31/mar
        for (var step = 1; dates.Count < horizon && step <= 400; step++)
        {
            var date = Advance(anchor, recurrence.Cadence, step);
            if (date.Date > today.Date)
                dates.Add(date);
        }

        return dates;
    }

    private static DetectedRecurrence? Analyze(
        TransactionType type,
        string key,
        IReadOnlyList<Transaction> occurrences,
        DateTime today)
    {
        if (occurrences.Count < MinYearly)
            return null;

        var dates = occurrences.Select(t => t.Date.Date).ToList();
        var intervals = new List<int>(dates.Count - 1);
        for (var i = 1; i < dates.Count; i++)
            intervals.Add((dates[i] - dates[i - 1]).Days);

        var cadence = ClassifyCadence(Median(intervals));
        if (cadence is null)
            return null;

        var (minOccurrences, low, high) = Window(cadence.Value);
        if (occurrences.Count < minOccurrences)
            return null;

        var regularShare = intervals.Count(d => d >= low && d <= high) / (double)intervals.Count;
        if (regularShare < MinRegularShare)
            return null;

        var amounts = occurrences.Select(t => t.Amount).ToList();
        var median = Median(amounts);
        if (median <= 0)
            return null;

        var deviation = (double)amounts.Max(a => Math.Abs(a - median) / median);
        if (deviation > VariableAmountTolerance)
            return null;

        var amountVaries = deviation > FixedAmountTolerance;

        var last = dates[^1];
        var nextExpected = Advance(last, cadence.Value, 1);

        // Passou muito do dia esperado sem nova ocorrência: a série provavelmente acabou (assinatura cancelada)
        if (today > nextExpected.AddDays(GraceDays(cadence.Value)))
            return null;

        // Valor fixo: o último pago (reflete um reajuste); variável: a mediana, estimativa mais justa para projetar
        var amount = amountVaries ? Math.Round(median, 2) : occurrences[^1].Amount;

        var kind = type == TransactionType.Income
            ? RecurringKind.Income
            : amountVaries ? RecurringKind.Bill : RecurringKind.Subscription;

        return new DetectedRecurrence(
            Key: key,
            Type: type,
            Cadence: cadence.Value,
            Kind: kind,
            Amount: amount,
            AmountVaries: amountVaries,
            FirstDate: dates[0],
            LastDate: last,
            NextExpectedDate: nextExpected,
            MonthlyCost: MonthlyCost(amount, cadence.Value),
            Confidence: Confidence(regularShare, occurrences.Count, minOccurrences),
            Occurrences: occurrences);
    }

    private static RecurrenceType? ClassifyCadence(double medianIntervalDays) => medianIntervalDays switch
    {
        >= 5 and <= 9 => RecurrenceType.Weekly,
        >= 25 and <= 36 => RecurrenceType.Monthly,
        >= 350 and <= 380 => RecurrenceType.Yearly,
        _ => null,
    };

    private static (int MinOccurrences, int Low, int High) Window(RecurrenceType cadence) => cadence switch
    {
        RecurrenceType.Weekly => (MinWeekly, 6, 8),
        RecurrenceType.Yearly => (MinYearly, 355, 375),
        _ => (MinMonthly, 26, 35),
    };

    private static int GraceDays(RecurrenceType cadence) => cadence switch
    {
        RecurrenceType.Weekly => WeeklyGraceDays,
        RecurrenceType.Yearly => YearlyGraceDays,
        _ => MonthlyGraceDays,
    };

    private static DateTime Advance(DateTime anchor, RecurrenceType cadence, int steps) => cadence switch
    {
        RecurrenceType.Weekly => anchor.AddDays(7 * steps),
        RecurrenceType.Yearly => anchor.AddYears(steps),
        _ => anchor.AddMonths(steps),
    };

    private static decimal MonthlyCost(decimal amount, RecurrenceType cadence) => cadence switch
    {
        RecurrenceType.Weekly => Math.Round(amount * 52m / 12m, 2),
        RecurrenceType.Yearly => Math.Round(amount / 12m, 2),
        _ => Math.Round(amount, 2),
    };

    // Mais ocorrências e intervalos mais regulares dão mais confiança; satura três ocorrências acima do mínimo
    private static double Confidence(double regularShare, int occurrences, int minOccurrences) =>
        Math.Round(regularShare * Math.Min(1.0, occurrences / (double)(minOccurrences + 3)), 2);

    private static double Median(List<int> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    private static decimal Median(List<decimal> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2m;
    }
}
