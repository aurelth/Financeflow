using FinanceFlow.Application.DTOs.Accounts;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;

namespace FinanceFlow.Application.Services;

public interface IAccountReconciliationService
{
    /// <summary>Concilia a conta com o último extrato confirmado que trouxe o saldo do banco.</summary>
    Task<AccountReconciliationDto> ReconcileAsync(Account account, CancellationToken cancellationToken = default);
}

public class AccountReconciliationService(
    IBankImportRepository bankImportRepository,
    IAccountRepository accountRepository) : IAccountReconciliationService
{
    public async Task<AccountReconciliationDto> ReconcileAsync(
        Account account,
        CancellationToken cancellationToken = default)
    {
        var statement = await bankImportRepository.GetLatestStatementAsync(
            account.UserId, account.Id, cancellationToken);

        if (statement?.LedgerBalance is not { } statementBalance ||
            statement.LedgerBalanceDate is not { } statementDate)
        {
            return Empty(account.Id, ReconciliationStatus.NoStatement,
                "Importe um extrato OFX com saldo para conciliar esta conta.");
        }

        // O saldo calculado só vale a partir do saldo inicial: antes dele não há como comparar
        if (account.InitialBalanceDate is { } initialDate && initialDate.Date > statementDate.Date)
        {
            return Empty(account.Id, ReconciliationStatus.Unavailable,
                "O saldo inicial da conta é posterior à data do extrato, então não há como comparar.",
                statement, statementBalance, statementDate);
        }

        var movements = await accountRepository.GetMovementTotalUntilAsync(
            account.UserId, account.Id, statementDate, cancellationToken);

        var calculated = account.InitialBalance + movements;
        var difference = Math.Round(statementBalance - calculated, 2, MidpointRounding.AwayFromZero);

        return new AccountReconciliationDto(
            AccountId: account.Id,
            Status: difference == 0 ? ReconciliationStatus.Matched : ReconciliationStatus.Divergent,
            ImportId: statement.Id,
            FileName: statement.FileName,
            StatementDate: statementDate,
            StatementStart: statement.StatementStart,
            StatementEnd: statement.StatementEnd,
            StatementBalance: statementBalance,
            CalculatedBalance: calculated,
            Difference: difference,
            Message: null);
    }

    private static AccountReconciliationDto Empty(
        Guid accountId,
        ReconciliationStatus status,
        string message,
        BankImport? statement = null,
        decimal? statementBalance = null,
        DateTime? statementDate = null) => new(
            AccountId: accountId,
            Status: status,
            ImportId: statement?.Id,
            FileName: statement?.FileName,
            StatementDate: statementDate,
            StatementStart: statement?.StatementStart,
            StatementEnd: statement?.StatementEnd,
            StatementBalance: statementBalance,
            CalculatedBalance: null,
            Difference: null,
            Message: message);
}
