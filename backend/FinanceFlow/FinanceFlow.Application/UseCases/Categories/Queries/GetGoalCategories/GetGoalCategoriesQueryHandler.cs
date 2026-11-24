using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using FinanceFlow.Application.Common.Mappings;

namespace FinanceFlow.Application.UseCases.Categories.Queries.GetGoalCategories;

public class GetGoalCategoriesQueryHandler(
    ICategoryRepository categoryRepository)
    : IRequestHandler<GetGoalCategoriesQuery, IEnumerable<CategoryDto>>
{
    public async Task<IEnumerable<CategoryDto>> Handle(
        GetGoalCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository
            .GetGoalCategoriesByUserAsync(request.UserId, cancellationToken);

        return categories.ToDtos();
    }
}
