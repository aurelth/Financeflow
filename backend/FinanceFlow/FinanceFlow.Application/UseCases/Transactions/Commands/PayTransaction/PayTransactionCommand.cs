using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transactions.Commands.PayTransaction;

/// <summary>Dá baixa: marca um lançamento Pendente/Agendado como Pago. Idempotente.</summary>
public record PayTransactionCommand(Guid Id, Guid UserId) : IRequest<TransactionDto>;
