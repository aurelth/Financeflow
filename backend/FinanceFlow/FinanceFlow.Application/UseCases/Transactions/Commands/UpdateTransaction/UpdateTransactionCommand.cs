using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
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
    bool PropagateToFuture = false,
    string? AttachmentPath = null,
    string? AttachmentName = null,
    Guid? AccountId = null,
    TransferDirection? TransferDirection = null
) : IRequest<TransactionDto>;
