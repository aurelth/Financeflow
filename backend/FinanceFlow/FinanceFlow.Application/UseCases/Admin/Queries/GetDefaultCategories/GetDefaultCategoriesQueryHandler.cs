using FinanceFlow.Application.DTOs.Admin;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Queries.GetDefaultCategories;

public class GetDefaultCategoriesQueryHandler(
    ICategoryRepository categoryRepository
) : IRequestHandler<GetDefaultCategoriesQuery, IEnumerable<AdminCategoryDto>>
{
    public async Task<IEnumerable<AdminCategoryDto>> Handle(
        GetDefaultCategoriesQuery request,
        CancellationToken cancellationToken)
    {
        var categories = await categoryRepository.GetAllDefaultAsync(cancellationToken);

        return categories.Select(c => new AdminCategoryDto(
            Id: c.Id,
            Name: c.Name,
            Icon: c.Icon,
            Color: c.Color,
            Type: c.Type,
            IsActive: c.IsActive,
            CreatedAt: c.CreatedAt
        ));
    }
}
