using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using FinanceFlow.Application.Common.Mappings;

namespace FinanceFlow.Application.UseCases.Categories.Queries.GetCategories;

public class GetCategoriesQueryHandler(
    ICategoryRepository categoryRepository)
    : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
{
    public async Task<IEnumerable<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository
            .GetAllByUserAsync(request.UserId, cancellationToken);

        return categories.ToDtos();
    }
}
