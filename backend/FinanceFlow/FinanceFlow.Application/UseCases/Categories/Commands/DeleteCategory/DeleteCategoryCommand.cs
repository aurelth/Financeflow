using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Commands.DeleteCategory;

public record DeleteCategoryCommand(
    Guid CategoryId,
    Guid UserId
) : IRequest;
