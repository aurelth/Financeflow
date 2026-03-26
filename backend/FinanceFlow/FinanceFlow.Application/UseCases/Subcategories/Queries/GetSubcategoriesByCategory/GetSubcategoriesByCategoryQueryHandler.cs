using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Subcategories.Queries.GetSubcategoriesByCategory;

public class GetSubcategoriesByCategoryQueryHandler(
    ICategoryRepository categoryRepository,
    ISubcategoryRepository subcategoryRepository,
    IMapper mapper)
    : IRequestHandler<GetSubcategoriesByCategoryQuery, IEnumerable<SubcategoryDto>>
{
    public async Task<IEnumerable<SubcategoryDto>> Handle(
        GetSubcategoriesByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        // Valida que a categoria existe e é acessível pelo utilizador
        var category = await categoryRepository
            .GetByIdAsync(request.CategoryId, request.UserId, cancellationToken);

        if (category is null)
            throw new NotFoundException("Categoria", request.CategoryId);

        var subcategories = await subcategoryRepository
            .GetByCategoryIdAsync(request.CategoryId, cancellationToken);

        return mapper.Map<IEnumerable<SubcategoryDto>>(subcategories);
    }
}
