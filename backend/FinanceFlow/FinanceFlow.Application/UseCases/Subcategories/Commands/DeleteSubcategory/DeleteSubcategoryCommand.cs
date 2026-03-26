using MediatR;

namespace FinanceFlow.Application.UseCases.Subcategories.Commands.DeleteSubcategory;

public record DeleteSubcategoryCommand(
    Guid SubcategoryId,
    Guid CategoryId,
    Guid UserId
) : IRequest;
