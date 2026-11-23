using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Domain.Enums;
using MediatR;

namespace FinanceFlow.Application.UseCases.Accounts.Commands.CreateAccount;

public record CreateAccountCommand(
    Guid UserId,
    string Name,
    AccountType Type,
    string? BankName,
    decimal InitialBalance,
    DateTime? InitialBalanceDate,
    string? Currency,
    string? Color,
    string? Icon
) : IRequest<AccountDto>;
