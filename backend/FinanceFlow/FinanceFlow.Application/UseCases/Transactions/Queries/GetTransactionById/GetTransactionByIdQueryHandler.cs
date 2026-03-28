using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Queries.GetTransactionById;

public class GetTransactionByIdQueryHandler(
    ITransactionRepository transactionRepository,
    IMapper mapper)
    : IRequestHandler<GetTransactionByIdQuery, TransactionDto>
{
    public async Task<TransactionDto> Handle(
        GetTransactionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.Id);

        return mapper.Map<TransactionDto>(transaction);
    }
}
