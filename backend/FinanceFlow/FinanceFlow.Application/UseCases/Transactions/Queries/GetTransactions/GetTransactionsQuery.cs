using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Queries.GetTransactions;

public record GetTransactionsQuery(
    Guid UserId,
    int Page = 1,
    int PageSize = 20,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    Guid? CategoryId = null,
    Guid? SubcategoryId = null,
    TransactionType? Type = null,
    TransactionStatus? Status = null,
    decimal? AmountMin = null,
    decimal? AmountMax = null,
    string? Search = null
) : IRequest<PagedResultDto<TransactionDto>>;
