using FinanceFlow.Domain.Entities;

namespace FinanceFlow.UnitTests.Common;

public static class CategoryBuilder
{
    // Categoria do utilizador (editável/deletável)
    public static Category Build(
        Guid? id = null,
        Guid? userId = null,
        string name = "Alimentação",
        string icon = "🍔",
        string color = "#6366f1",
        TransactionType type = TransactionType.Expense,
        bool isDefault = false,
        bool isActive = true) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = userId ?? Guid.NewGuid(),
            Name = name,
            Icon = icon,
            Color = color,
            Type = type,
            IsDefault = isDefault,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            Subcategories = []
        };

    // Categoria padrão do sistema (UserId = null)
    public static Category BuildDefault(
        Guid? id = null,
        string name = "Salário",
        TransactionType type = TransactionType.Income) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            UserId = null,
            Name = name,
            Icon = "💰",
            Color = "#22c55e",
            Type = type,
            IsDefault = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Subcategories = []
        };
}
