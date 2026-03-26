using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Subcategories.Commands.UpdateSubcategory;

public record UpdateSubcategoryCommand(
    Guid SubcategoryId,
    Guid CategoryId,
    Guid UserId,
    string Name
) : IRequest<SubcategoryDto>;
