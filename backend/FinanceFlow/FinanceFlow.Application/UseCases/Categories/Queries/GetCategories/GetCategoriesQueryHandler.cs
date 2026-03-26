using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Queries.GetCategories;

public class GetCategoriesQueryHandler(
    ICategoryRepository categoryRepository,
    IMapper mapper)
    : IRequestHandler<GetCategoriesQuery, IEnumerable<CategoryDto>>
{
    public async Task<IEnumerable<CategoryDto>> Handle(
        GetCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository
            .GetAllByUserAsync(request.UserId, cancellationToken);

        return mapper.Map<IEnumerable<CategoryDto>>(categories);
    }
}
