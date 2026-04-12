using FinanceFlow.Application.DTOs.Admin;
using FinanceFlow.Domain.Entities;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.CreateDefaultCategory;

public record CreateDefaultCategoryCommand(
    string Name,
    string Icon,
    string Color,
    TransactionType Type
) : IRequest<AdminCategoryDto>;
