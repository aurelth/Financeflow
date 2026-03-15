namespace FinanceFlow.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = "#6366f1";
    public TransactionType Type { get; set; }
    public bool IsDefault { get; set; } = false;
    public bool IsActive { get; set; } = true;

    public Guid? UserId { get; set; }   // null = categoria padrão do sistema
    public User? User { get; set; }

    public ICollection<Subcategory> Subcategories { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
}
