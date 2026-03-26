using AutoMapper;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Queries.GetTransactions;

public class GetTransactionsQueryHandler(
    ITransactionRepository transactionRepository,
    IMapper mapper)
    : IRequestHandler<GetTransactionsQuery, PagedResultDto<TransactionDto>>
{
    public async Task<PagedResultDto<TransactionDto>> Handle(
        GetTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await transactionRepository.GetPagedByUserAsync(
            userId: request.UserId,
            page: request.Page,
            pageSize: request.PageSize,
            dateFrom: request.DateFrom,
            dateTo: request.DateTo,
            categoryId: request.CategoryId,
            subcategoryId: request.SubcategoryId,
            type: request.Type,
            status: request.Status,
            amountMin: request.AmountMin,
            amountMax: request.AmountMax,
            search: request.Search,
            cancellationToken: cancellationToken);

        var dtos = mapper.Map<IEnumerable<TransactionDto>>(items);
        var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

        return new PagedResultDto<TransactionDto>(
            Items: dtos,
            TotalCount: totalCount,
            Page: request.Page,
            PageSize: request.PageSize,
            TotalPages: totalPages);
    }
}
