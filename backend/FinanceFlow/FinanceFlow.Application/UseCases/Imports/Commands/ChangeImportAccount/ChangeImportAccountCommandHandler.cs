using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Imports.Commands.ChangeImportAccount;

/// <summary>
/// Troca a conta de uma importação ainda não confirmada e refaz a detecção de duplicatas,
/// já que "duplicado" só faz sentido dentro de uma conta.
/// </summary>
public class ChangeImportAccountCommandHandler(
    IBankImportRepository bankImportRepository,
    IAccountRepository accountRepository,
    IImportDeduplicationService deduplicationService,
    ITransferMatchingService transferMatchingService)
    : IRequestHandler<ChangeImportAccountCommand, BankImportDto>
{
    public async Task<BankImportDto> Handle(
        ChangeImportAccountCommand request,
        CancellationToken cancellationToken)
    {
        var bankImport = await bankImportRepository.GetByIdAsync(
            request.ImportId, request.UserId, cancellationToken)
            ?? throw new NotFoundException("BankImport", request.ImportId);

        if (bankImport.Imported > 0)
            throw new ValidationException("Esta importação já foi confirmada e não pode mudar de conta.");

        if (bankImport.Status == BankImportStatus.Processing)
            throw new ValidationException("A importação ainda está sendo processada. Tente novamente em instantes.");

        var account = await accountRepository.GetByIdAsync(
            request.AccountId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        if (account.IsArchived)
            throw new ValidationException("Não é possível importar para uma conta arquivada.");

        if (account.Id != bankImport.AccountId)
        {
            await LearnAccountKeyAsync(bankImport, account, request.UserId, cancellationToken);

            bankImport.AccountId = account.Id;
            bankImport.Account = account;
        }

        // Pendente: o Worker ainda vai processar e já usará a nova conta
        // O par de uma transferência também depende da conta (a outra ponta precisa estar em OUTRA conta)
        if (bankImport.Status == BankImportStatus.Completed)
        {
            bankImport.Duplicates = await deduplicationService.DetectAsync(bankImport, cancellationToken);
            await transferMatchingService.MatchAsync(bankImport, cancellationToken);
        }

        await bankImportRepository.UpdateAsync(bankImport, cancellationToken);

        return new BankImportDto(
            Id: bankImport.Id,
            FileName: bankImport.FileName,
            Status: bankImport.Status,
            TotalRecords: bankImport.TotalRecords,
            Imported: bankImport.Imported,
            Duplicates: bankImport.Duplicates,
            Errors: bankImport.Errors,
            ErrorMessage: bankImport.ErrorMessage,
            CreatedAt: bankImport.CreatedAt,
            AccountId: account.Id,
            AccountName: account.Name,
            AccountResolution: AccountResolutions.Provided);
    }

    // Mesma regra do upload: o arquivo não pode ir para uma conta diferente da que já o reconhece,
    // e uma conta ainda sem vínculo passa a reconhecê-lo nas próximas importações
    private async Task LearnAccountKeyAsync(
        BankImport bankImport,
        Account chosen,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(bankImport.AccountKeyHash))
            return;

        var owner = await accountRepository.GetByKeyAsync(
            userId, bankImport.BankId, bankImport.AccountKeyHash, cancellationToken);

        if (owner is not null && owner.Id != chosen.Id)
            throw new ValidationException(
                $"Este arquivo pertence à conta \"{owner.Name}\". Selecione essa conta para importar.");

        if (string.IsNullOrEmpty(chosen.AccountKeyHash))
        {
            chosen.AccountKeyHash = bankImport.AccountKeyHash;
            chosen.BankId ??= bankImport.BankId;
            chosen.AccountLast4 = bankImport.AccountLast4;
            await accountRepository.UpdateAsync(chosen, cancellationToken);
        }
    }
}
