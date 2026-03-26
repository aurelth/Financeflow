using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Application.Events;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.CreateTransaction;

public class CreateTransactionCommandHandler(
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IEventPublisher eventPublisher,
    IConfiguration configuration,
    IMapper mapper)
    : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        // Valida que a categoria existe e pertence ao utilizador
        var category = await categoryRepository.GetByIdAsync(
            request.CategoryId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        // Valida que o tipo da transação coincide com o tipo da categoria
        if (category.Type != request.Type)
            throw new ValidationException(
                "O tipo da transação não coincide com o tipo da categoria.");

        var transaction = new Transaction
        {
            UserId = request.UserId,
            Amount = request.Amount,
            Type = request.Type,
            Date = request.Date,
            Description = request.Description,
            Status = request.Status,
            IsRecurring = request.IsRecurring,
            RecurrenceType = request.RecurrenceType,
            CategoryId = request.CategoryId,
            SubcategoryId = request.SubcategoryId,
            Tags = JsonSerializer.Serialize(request.Tags),
        };

        await transactionRepository.AddAsync(transaction, cancellationToken);

        // Publica evento Kafka
        var topic = configuration["Kafka:Topics:TransactionCreated"]
                    ?? "finance.transactions.created";

        await eventPublisher.PublishAsync(topic, new TransactionCreatedEvent(
            TransactionId: transaction.Id,
            UserId: transaction.UserId,
            Amount: transaction.Amount,
            Type: transaction.Type,
            Date: transaction.Date,
            Description: transaction.Description,
            Status: transaction.Status,
            CategoryId: transaction.CategoryId,
            CategoryName: category.Name,
            CreatedAt: transaction.CreatedAt),
            cancellationToken);

        // Recarrega com as navegações para o mapeamento
        var created = await transactionRepository.GetByIdAsync(
            transaction.Id, request.UserId, cancellationToken);

        return mapper.Map<TransactionDto>(created);
    }
}
