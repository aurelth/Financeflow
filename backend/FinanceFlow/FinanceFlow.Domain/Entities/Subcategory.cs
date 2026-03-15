namespace FinanceFlow.Domain.Entities;

public class Subcategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<Transaction> Transactions { get; set; } = [];
}
