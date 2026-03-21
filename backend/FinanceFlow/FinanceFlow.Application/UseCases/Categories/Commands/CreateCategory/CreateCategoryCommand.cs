using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Commands.CreateCategory;

public record CreateCategoryCommand(
    Guid UserId,
    string Name,
    string Icon,
    string Color,
    TransactionType Type
) : IRequest<CategoryDto>;
