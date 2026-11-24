using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Transfers.Commands.UnlinkTransfer;

/// <summary>
/// Desfaz uma transferência. Sem o vínculo, cada ponta é só um lançamento de uma conta:
/// ou volta a ser despesa/receita (o usuário escolhe a categoria, pois a original se perdeu ao virar
/// transferência) ou é excluída junto com a outra.
/// </summary>
public class UnlinkTransferCommandHandler(
    ITransferRepository transferRepository,
    ICategoryRepository categoryRepository,
    IDashboardCacheInvalidator dashboardCacheInvalidator)
    : IRequestHandler<UnlinkTransferCommand>
{
    public async Task Handle(UnlinkTransferCommand request, CancellationToken cancellationToken)
    {
        var link = await transferRepository.GetLinkAsync(request.LinkId, request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(TransferLink), request.LinkId);

        var outTransaction = link.OutTransaction;
        var inTransaction = link.InTransaction;

        if (request.DeleteTransactions)
        {
            var now = DateTime.UtcNow;
            outTransaction.DeletedAt = now;
            inTransaction.DeletedAt = now;
        }
        else
        {
            var outCategory = await RequireCategoryAsync(
                request.OutCategoryId, TransactionType.Expense, "saída", request.UserId, cancellationToken);
            var inCategory = await RequireCategoryAsync(
                request.InCategoryId, TransactionType.Income, "entrada", request.UserId, cancellationToken);

            Revert(outTransaction, TransactionType.Expense, outCategory.Id);
            Revert(inTransaction, TransactionType.Income, inCategory.Id);

            // Quem desfez disse que isso não é uma transferência: não volta a sugerir o mesmo par
            transferRepository.AddRejection(
                TransferRejection.Create(request.UserId, outTransaction.Id, inTransaction.Id));
        }

        transferRepository.RemoveLink(link);
        await transferRepository.SaveChangesAsync(cancellationToken);

        await dashboardCacheInvalidator.InvalidateAsync(
            request.UserId, [outTransaction.Date, inTransaction.Date], cancellationToken);
    }

    private async Task<Category> RequireCategoryAsync(
        Guid? categoryId,
        TransactionType expectedType,
        string sideName,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (categoryId is null || categoryId == Guid.Empty)
            throw new ValidationException($"Escolha a categoria da ponta de {sideName}.");

        var category = await categoryRepository.GetByIdAsync(categoryId.Value, userId, cancellationToken)
            ?? throw new NotFoundException(nameof(Category), categoryId.Value);

        if (category.Type != expectedType)
        {
            var kind = expectedType == TransactionType.Expense ? "despesa" : "receita";
            throw new ValidationException($"A categoria da ponta de {sideName} precisa ser de {kind}.");
        }

        return category;
    }

    private static void Revert(Transaction transaction, TransactionType type, Guid categoryId)
    {
        transaction.Type = type;
        transaction.TransferDirection = null;
        transaction.CategoryId = categoryId;
        transaction.SubcategoryId = null;
        transaction.UpdatedAt = DateTime.UtcNow;
    }
}
