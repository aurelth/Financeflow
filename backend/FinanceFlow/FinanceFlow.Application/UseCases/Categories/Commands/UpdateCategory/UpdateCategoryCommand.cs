using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Commands.UpdateCategory;

public record UpdateCategoryCommand(
    Guid CategoryId,
    Guid UserId,
    string Name,
    string Icon,
    string Color
) : IRequest<CategoryDto>;
