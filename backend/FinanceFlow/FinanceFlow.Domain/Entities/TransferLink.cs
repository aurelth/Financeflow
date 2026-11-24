using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Domain.Entities;

/// <summary>
/// Liga as duas pontas de uma transferência entre contas do mesmo usuário:
/// a saída (conta de origem) e a entrada (conta de destino).
/// Cada lançamento participa de no máximo um vínculo.
/// </summary>
public class TransferLink : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid OutTransactionId { get; set; }
    public Transaction OutTransaction { get; set; } = null!;

    public Guid InTransactionId { get; set; }
    public Transaction InTransaction { get; set; } = null!;

    public Enums.TransferLinkOrigin Origin { get; set; }

    /// <summary>Pontuação do pareamento automático/sugerido (nulo para vínculos manuais).</summary>
    public int? Score { get; set; }
}
