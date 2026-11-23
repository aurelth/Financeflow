using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs.Accounts;

public record CreateAccountRequestDto(
    string Name,
    AccountType Type,
    string? BankName,
    decimal InitialBalance,
    DateTime? InitialBalanceDate,
    string? Currency,
    string? Color,
    string? Icon);

public record UpdateAccountRequestDto(
    string Name,
    AccountType Type,
    string? BankName,
    decimal InitialBalance,
    DateTime? InitialBalanceDate,
    string? Currency,
    string? Color,
    string? Icon,
    bool IsArchived = false);
