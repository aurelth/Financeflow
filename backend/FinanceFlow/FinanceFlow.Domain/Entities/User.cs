namespace FinanceFlow.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string Currency { get; set; } = "BRL";
    public string Timezone { get; set; } = "America/Sao_Paulo";

    // Navegação
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<Report> Reports { get; set; } = [];
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = [];
}
