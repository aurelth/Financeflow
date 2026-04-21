namespace FinanceFlow.Domain.Entities;

public class Goal : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal TargetAmount { get; set; }
    public decimal MonthlyContribution { get; set; }
    public DateTime Deadline { get; set; }
    public string Emoji { get; set; } = "🎯";
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}
