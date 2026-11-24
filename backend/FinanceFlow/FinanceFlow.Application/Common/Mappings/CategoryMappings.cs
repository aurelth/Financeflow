using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Common.Mappings;

/// <summary>Mapeamento de Category/Subcategory para seus DTOs.</summary>
public static class CategoryMappings
{
    public static SubcategoryDto ToDto(this Subcategory subcategory) => new(
        Id:       subcategory.Id,
        Name:     subcategory.Name,
        IsActive: subcategory.IsActive);

    public static List<SubcategoryDto> ToDtos(this IEnumerable<Subcategory> subcategories) =>
        subcategories.Select(ToDto).ToList();

    public static CategoryDto ToDto(this Category category) => new(
        Id:             category.Id,
        Name:           category.Name,
        Icon:           category.Icon,
        Color:          category.Color,
        Type:           category.Type,
        IsDefault:      category.IsDefault,
        IsActive:       category.IsActive,
        // UserId nulo = categoria padrão do sistema; preenchido = criada pelo utilizador
        IsOwner:        category.UserId != null,
        IsGoalCategory: category.IsGoalCategory,
        // Só subcategorias vivas e ativas, em ordem alfabética
        Subcategories:  category.Subcategories
            .Where(s => s.DeletedAt == null && s.IsActive)
            .OrderBy(s => s.Name)
            .ToDtos());

    public static List<CategoryDto> ToDtos(this IEnumerable<Category> categories) =>
        categories.Select(ToDto).ToList();
}
