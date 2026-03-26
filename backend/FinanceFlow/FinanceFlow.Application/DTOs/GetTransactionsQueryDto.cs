using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs;

public record GetTransactionsQueryDto(
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
);
