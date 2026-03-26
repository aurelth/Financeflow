using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Subcategories.Commands.CreateSubcategory;

public record CreateSubcategoryCommand(
    Guid CategoryId,
    Guid UserId,
    string Name
) : IRequest<SubcategoryDto>;
