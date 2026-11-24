using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Common.Mappings;

/// <summary>
/// Mapeamento Budget → BudgetDto. Exige <see cref="Budget.Category"/> carregada
/// (os repositórios fazem o Include): sem ela falha na hora, em vez de devolver nomes nulos.
/// </summary>
public static class BudgetMappings
{
    public static BudgetDto ToDto(this Budget budget) => new(
        Id:           budget.Id,
        CategoryId:   budget.CategoryId,
        CategoryName: budget.Category.Name,
        CategoryIcon: budget.Category.Icon,
        CategoryColor: budget.Category.Color,
        Month:        budget.Month,
        Year:         budget.Year,
        LimitAmount:  budget.LimitAmount,
        CreatedAt:    budget.CreatedAt,
        UpdatedAt:    budget.UpdatedAt);

    public static List<BudgetDto> ToDtos(this IEnumerable<Budget> budgets) =>
        budgets.Select(ToDto).ToList();
}
