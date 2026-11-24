namespace FinanceFlow.Application.Common.Exceptions;

/// <summary>O usuário esgotou a cota diária de uma função paga por uso (IA). Vira HTTP 429.</summary>
public class QuotaExceededException(string message, int limit, DateTime resetsAtUtc) : Exception(message)
{
    public int Limit { get; } = limit;

    /// <summary>Quando a cota volta a ficar disponível (próxima meia-noite do usuário), em UTC.</summary>
    public DateTime ResetsAtUtc { get; } = resetsAtUtc;
}
