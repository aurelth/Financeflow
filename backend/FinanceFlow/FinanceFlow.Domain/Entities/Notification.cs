namespace FinanceFlow.Domain.Entities;

public class Notification : BaseEntity
{
    public string Type { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; } = false;

    public Guid UserId { get; set; }
    public Guid? ReferenceId { get; set; }
    public User User { get; set; } = null!;
}
