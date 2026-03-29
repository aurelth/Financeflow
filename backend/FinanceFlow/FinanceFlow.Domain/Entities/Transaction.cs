namespace FinanceFlow.Domain.Entities;

public class Transaction : BaseEntity
{
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public TransactionStatus Status { get; set; } = TransactionStatus.Paid;
    public bool IsRecurring { get; set; } = false;
    public RecurrenceType RecurrenceType { get; set; } = RecurrenceType.None;
    public string? AttachmentPath { get; set; }
    public string? AttachmentName { get; set; }
    public string Tags { get; set; } = string.Empty; // JSON serializado

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public Guid? SubcategoryId { get; set; }
    public Subcategory? Subcategory { get; set; }
}
