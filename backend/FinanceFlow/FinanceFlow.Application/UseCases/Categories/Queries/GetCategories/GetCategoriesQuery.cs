using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Queries.GetCategories;

public record GetCategoriesQuery(Guid UserId) : IRequest<IEnumerable<CategoryDto>>;
