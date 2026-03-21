using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Queries.GetCategoryById;

public class GetCategoryByIdQueryHandler(
    ICategoryRepository categoryRepository,
    IMapper mapper)
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

        return mapper.Map<CategoryDto>(category);
    }
}
