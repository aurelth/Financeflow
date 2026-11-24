using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Domain.Entities;

/// <summary>
/// Regra aprendida: "descrição normalizada → categoria", por usuário e por tipo (receita/despesa).
/// Nasce quando o usuário confirma uma importação e é aplicada nas próximas.
/// A última escolha do usuário sempre vence; <see cref="HitCount"/> conta quantas vezes ela foi confirmada.
/// </summary>
public class CategoryRule : BaseEntity
{
    public const int MaxKeyLength = 120;

    public Guid UserId { get; set; }

    /// <summary>Descrição já normalizada (ver DescriptionNormalizer). Nunca vazia.</summary>
    public string Key { get; set; } = string.Empty;

    public TransactionType Type { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    /// <summary>Quantas vezes o usuário confirmou esta mesma escolha.</summary>
    public int HitCount { get; set; } = 1;

    public DateTime LastUsedAt { get; set; } = DateTime.UtcNow;

    public static CategoryRule Create(Guid userId, TransactionType type, string key, Guid categoryId, int hits = 1) =>
        new()
        {
            UserId = userId,
            Type = type,
            Key = key,
            CategoryId = categoryId,
            HitCount = hits,
        };

    /// <summary>Registra uma nova confirmação. Trocar de categoria recomeça a contagem: a regra antiga deixou de valer.</summary>
    public void Reinforce(Guid categoryId, int times = 1)
    {
        if (CategoryId == categoryId)
        {
            HitCount += times;
        }
        else
        {
            // Só a FK muda: o EF reposiciona a navegação (anulá-la numa relação obrigatória quebraria o SaveChanges)
            CategoryId = categoryId;
            HitCount = times;
        }

        LastUsedAt = DateTime.UtcNow;
    }
}
