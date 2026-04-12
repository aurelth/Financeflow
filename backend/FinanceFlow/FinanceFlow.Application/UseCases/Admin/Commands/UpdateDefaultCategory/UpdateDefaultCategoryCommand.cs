using FinanceFlow.Application.DTOs.Admin;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.UpdateDefaultCategory;

public record UpdateDefaultCategoryCommand(
    Guid Id,
    string Name,
    string Icon,
    string Color
) : IRequest<AdminCategoryDto>;
