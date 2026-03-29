using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.CreateTransaction;

public record CreateTransactionCommand(
    Guid UserId,
    decimal Amount,
    TransactionType Type,
    DateTime Date,
    string Description,
    TransactionStatus Status,
    bool IsRecurring,
    RecurrenceType RecurrenceType,
    Guid CategoryId,
    Guid? SubcategoryId,    
    string[] Tags,
    // Anexo opcional
    string? AttachmentName = null,
    Stream? AttachmentStream = null,
    string? AttachmentFileName = null,
    string? AttachmentContentType = null
) : IRequest<TransactionDto>;
