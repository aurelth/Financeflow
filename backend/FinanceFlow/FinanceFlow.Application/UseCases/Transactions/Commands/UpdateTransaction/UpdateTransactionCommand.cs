using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.UpdateTransaction;

public record UpdateTransactionCommand(
    Guid Id,
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
    string? AttachmentPath = null,
    string? AttachmentName = null
) : IRequest<TransactionDto>;
