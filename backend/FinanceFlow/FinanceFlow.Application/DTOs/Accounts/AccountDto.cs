using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;

namespace FinanceFlow.Application.DTOs.Accounts;

public record AccountDto(
    Guid Id,
    string Name,
    AccountType Type,
    string? BankId,
    string? BankName,
    string? AccountLast4,
    bool HasBankKey,
    decimal InitialBalance,
    DateTime? InitialBalanceDate,
    string Currency,
    string Color,
    string Icon,
    bool IsDefault,
    bool IsArchived,
    decimal Balance,
    DateTime CreatedAt)
{
    public static AccountDto From(Account account, decimal movementTotal) => new(
        Id: account.Id,
        Name: account.Name,
        Type: account.Type,
        BankId: account.BankId,
        BankName: account.BankName,
        AccountLast4: account.AccountLast4,
        HasBankKey: !string.IsNullOrEmpty(account.AccountKeyHash),
        InitialBalance: account.InitialBalance,
        InitialBalanceDate: account.InitialBalanceDate,
        Currency: account.Currency,
        Color: account.Color,
        Icon: account.Icon,
        IsDefault: account.IsDefault,
        IsArchived: account.IsArchived,
        Balance: account.InitialBalance + movementTotal,
        CreatedAt: account.CreatedAt);
}
