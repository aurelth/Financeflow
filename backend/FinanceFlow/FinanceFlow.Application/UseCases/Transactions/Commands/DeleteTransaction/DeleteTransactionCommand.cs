using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.DeleteTransaction;

public record DeleteTransactionCommand(
    Guid Id,
    Guid UserId
) : IRequest;
