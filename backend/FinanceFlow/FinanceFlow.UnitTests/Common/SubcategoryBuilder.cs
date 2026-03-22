using FinanceFlow.Domain.Entities;

namespace FinanceFlow.UnitTests.Common;

public static class SubcategoryBuilder
{
    public static Subcategory Build(
        Guid? id = null,
        Guid? categoryId = null,
        string name = "Restaurante",
        bool isActive = true) =>
        new()
        {
            Id = id ?? Guid.NewGuid(),
            CategoryId = categoryId ?? Guid.NewGuid(),
            Name = name,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
}
