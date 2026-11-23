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
    // Conta (opcional: sem ela, usa a "Conta principal")
    Guid? AccountId = null,
    // Anexo opcional
    string? AttachmentName = null,
    Stream? AttachmentStream = null,
    string? AttachmentFileName = null,
    string? AttachmentContentType = null
) : IRequest<TransactionDto>;
