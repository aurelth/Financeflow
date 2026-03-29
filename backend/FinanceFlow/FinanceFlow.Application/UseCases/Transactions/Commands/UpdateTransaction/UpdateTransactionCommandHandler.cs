using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.UpdateTransaction;

public class UpdateTransactionCommandHandler(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IMapper mapper)
    : IRequestHandler<UpdateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(
        UpdateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var transaction = await transactionRepository.GetByIdAsync(
            request.Id, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Transaction), request.Id);

        var category = await categoryRepository.GetByIdAsync(
            request.CategoryId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        if (category.Type != request.Type)
            throw new ValidationException(
                "O tipo da transação não coincide com o tipo da categoria.");

        transaction.Amount = request.Amount;
        transaction.Type = request.Type;
        transaction.Date = request.Date;
        transaction.Description = request.Description;
        transaction.Status = request.Status;
        transaction.IsRecurring = request.IsRecurring;
        transaction.RecurrenceType = request.RecurrenceType;
        transaction.CategoryId = request.CategoryId;
        transaction.SubcategoryId = request.SubcategoryId;
        transaction.Tags = JsonSerializer.Serialize(request.Tags);

        if (request.AttachmentPath != null)
            transaction.AttachmentPath = request.AttachmentPath;

        if (request.AttachmentName != null)
            transaction.AttachmentName = request.AttachmentName;

        await transactionRepository.UpdateAsync(transaction, cancellationToken);

        var updated = await transactionRepository.GetByIdAsync(
            transaction.Id, request.UserId, cancellationToken);

        return mapper.Map<TransactionDto>(updated);
    }
}
