namespace FinanceFlow.Domain.Entities;

public class Budget : BaseEntity
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal LimitAmount { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
