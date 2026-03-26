using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Queries.GetCategoryById;

public record GetCategoryByIdQuery(
    Guid CategoryId,
    Guid UserId
) : IRequest<CategoryDto>;
