using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Subcategories.Queries.GetSubcategoriesByCategory;

public record GetSubcategoriesByCategoryQuery(
    Guid CategoryId,
    Guid UserId
) : IRequest<IEnumerable<SubcategoryDto>>;
