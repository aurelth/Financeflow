using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Domain.Entities;

/// <summary>
/// "Isso não é uma assinatura": o usuário dispensou a sugestão de recorrência para esta descrição.
/// A detecção deixa de oferecê-la; é a única coisa persistida pela detecção, que de resto é recalculada
/// a cada consulta a partir do histórico (assim nunca fica defasada em relação aos lançamentos).
/// </summary>
public class DismissedRecurrence : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>Descrição normalizada (ver DescriptionNormalizer). Nunca vazia.</summary>
    public string Key { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    public static DismissedRecurrence Create(Guid userId, TransactionType type, string key) =>
        new() { UserId = userId, Type = type, Key = key };
}
