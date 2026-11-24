namespace FinanceFlow.Domain.Entities;

/// <summary>
/// Par de lançamentos que o usuário disse NÃO serem uma transferência.
/// Evita que a reavaliação das pontas sem par volte a sugerir o mesmo pareamento.
/// Os ids ficam em ordem (o menor primeiro) para o par ser único independente da direção.
/// </summary>
public class TransferRejection : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid FirstTransactionId { get; set; }
    public Guid SecondTransactionId { get; set; }

    public static TransferRejection Create(Guid userId, Guid a, Guid b)
    {
        var (first, second) = a.CompareTo(b) <= 0 ? (a, b) : (b, a);
        return new TransferRejection { UserId = userId, FirstTransactionId = first, SecondTransactionId = second };
    }
}
