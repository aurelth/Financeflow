using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Queries.GetGoalCategories;

public class GetGoalCategoriesQueryHandler(
    ICategoryRepository categoryRepository,
    IMapper mapper)
    : IRequestHandler<GetGoalCategoriesQuery, IEnumerable<CategoryDto>>
{
    public async Task<IEnumerable<CategoryDto>> Handle(
        GetGoalCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository
            .GetGoalCategoriesByUserAsync(request.UserId, cancellationToken);

        return mapper.Map<IEnumerable<CategoryDto>>(categories);
    }
}
