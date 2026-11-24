namespace FinanceFlow.Domain.Entities;

/// <summary>
/// Quantas chamadas de IA o usuário já consumiu em um dia (no fuso horário dele).
/// Uma linha por usuário e dia, incrementada de forma atômica pelo repositório.
/// Fica no banco, e não no cache, para sobreviver a reinícios e servir de base para planos e cobrança.
/// </summary>
public class AiUsage : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>Dia civil no fuso horário do usuário.</summary>
    public DateOnly Day { get; set; }

    public int Count { get; set; }
}
