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
    IAttachmentService attachmentService,
    IConfiguration configuration,
    ICacheService cache,
    IMapper mapper)
    : IRequestHandler<CreateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(
        CreateTransactionCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetByIdAsync(
            request.CategoryId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), request.CategoryId);

        if (category.Type != request.Type)
            throw new ValidationException(
                "O tipo da transação não coincide com o tipo da categoria.");

        // Processa o anexo se fornecido
        string? attachmentPath = null;
        string? attachmentName = null;
        if (request.AttachmentStream != null &&
            request.AttachmentFileName != null &&
            request.AttachmentContentType != null)
        {
            var result = await attachmentService.SaveAsync(
                request.AttachmentStream,
                request.AttachmentFileName,
                request.AttachmentContentType,
                request.UserId,
                cancellationToken);
            attachmentPath = result.Path;
            attachmentName = result.Name;
        }

        // Gera o RecurrenceGroupId se a transação for recorrente
        Guid? recurrenceGroupId = request.IsRecurring ? Guid.NewGuid() : null;

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
            AttachmentPath = attachmentPath,
            AttachmentName = attachmentName,
            RecurrenceGroupId = recurrenceGroupId,
        };

        await transactionRepository.AddAsync(transaction, cancellationToken);

        // Gera cópias para os meses restantes do ano se recorrente
        if (request.IsRecurring)
        {
            var copies = GerarCopiasRecorrentes(transaction, request.Tags);
            foreach (var copy in copies)
                await transactionRepository.AddAsync(copy, cancellationToken);
        }

        // Invalida o cache do dashboard para o mês da transação
        await InvalidarCacheDashboardAsync(request.UserId, request.Date, cancellationToken);

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

        var created = await transactionRepository.GetByIdAsync(
            transaction.Id, request.UserId, cancellationToken);

        return mapper.Map<TransactionDto>(created);
    }

    // Gera cópias mensais para os meses restantes do ano corrente
    private static List<Transaction> GerarCopiasRecorrentes(
        Transaction origem,
        string[] tags)
    {
        var copies = new List<Transaction>();
        var mesAtual = origem.Date.Month;
        var ano = origem.Date.Year;

        for (var mes = mesAtual + 1; mes <= 12; mes++)
        {
            // Garante que o dia é válido para o mês de destino
            var diasNoMes = DateTime.DaysInMonth(ano, mes);
            var dia = Math.Min(origem.Date.Day, diasNoMes);

            copies.Add(new Transaction
            {
                UserId = origem.UserId,
                Amount = origem.Amount,
                Type = origem.Type,
                Date = new DateTime(ano, mes, dia, origem.Date.Hour, origem.Date.Minute, origem.Date.Second),
                Description = origem.Description,
                Status = TransactionStatus.Scheduled,
                IsRecurring = true,
                RecurrenceType = origem.RecurrenceType,
                RecurrenceGroupId = origem.RecurrenceGroupId,
                CategoryId = origem.CategoryId,
                SubcategoryId = origem.SubcategoryId,
                Tags = JsonSerializer.Serialize(tags),
                AttachmentPath = null,
                AttachmentName = null,
            });
        }

        return copies;
    }

    // Invalida todas as chaves de cache do dashboard para o mês/ano da transação
    private async Task InvalidarCacheDashboardAsync(
        Guid userId,
        DateTime date,
        CancellationToken cancellationToken)
    {
        var prefixes = new[]
        {
            $"dashboard:summary:{userId}:{date.Year}:{date.Month}",
            $"dashboard:balance-evolution:{userId}:{date.Year}:{date.Month}",
            $"dashboard:expenses-by-category:{userId}:{date.Year}:{date.Month}",
            $"dashboard:weekly-comparison:{userId}:{date.Year}:{date.Month}",
        };

        foreach (var prefix in prefixes)
            await cache.RemoveAsync(prefix, cancellationToken);
    }
}
