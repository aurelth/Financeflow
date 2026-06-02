using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Queries.GetGoalCategories;

public record GetGoalCategoriesQuery(Guid UserId) : IRequest<IEnumerable<CategoryDto>>;
