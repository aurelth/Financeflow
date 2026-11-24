namespace FinanceFlow.Domain.Entities;

public class UserNotificationPreferences : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    // Alertas de orçamento
    public bool BudgetWarningEnabled { get; set; } = true;   // 80%
    public bool BudgetCriticalEnabled { get; set; } = true;  // 100%

    // Alertas de vencimento de transações
    public bool TransactionDueTomorrowEnabled { get; set; } = true;
    public bool TransactionDueIn3DaysEnabled { get; set; } = true;

    // Lembrete no próprio dia do vencimento e aviso (uma vez) de conta que venceu sem ser paga
    public bool TransactionDueTodayEnabled { get; set; } = true;
    public bool TransactionOverdueEnabled { get; set; } = true;
}
