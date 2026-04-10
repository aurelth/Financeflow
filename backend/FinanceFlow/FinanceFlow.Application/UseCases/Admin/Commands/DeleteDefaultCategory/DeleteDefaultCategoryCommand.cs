using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.DeleteDefaultCategory;

public record DeleteDefaultCategoryCommand(Guid Id) : IRequest;
