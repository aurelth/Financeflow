using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using FinanceFlow.Application.Common.Mappings;

namespace FinanceFlow.Application.UseCases.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler(
    ICategoryRepository categoryRepository)
    : IRequestHandler<GetCategoryByIdQuery, CategoryDto>
{
    public async Task<CategoryDto> Handle(
        GetCategoryByIdQuery request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository
            .GetByIdAsync(request.CategoryId, request.UserId, cancellationToken);

        if (category is null)
            throw new NotFoundException("Categoria", request.CategoryId);

        return category.ToDto();
    }
}
