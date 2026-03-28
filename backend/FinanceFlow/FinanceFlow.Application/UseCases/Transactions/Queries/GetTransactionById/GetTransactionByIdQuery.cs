using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Queries.GetTransactionById;

public record GetTransactionByIdQuery(
    Guid Id,
    Guid UserId
) : IRequest<TransactionDto>;
