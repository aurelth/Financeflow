using FinanceFlow.Application.DTOs.Admin;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Queries.GetDefaultCategories;

public record GetDefaultCategoriesQuery : IRequest<IEnumerable<AdminCategoryDto>>;
