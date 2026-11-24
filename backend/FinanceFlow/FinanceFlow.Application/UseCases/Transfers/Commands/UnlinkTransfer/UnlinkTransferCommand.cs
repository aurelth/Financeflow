using MediatR;

namespace FinanceFlow.Application.UseCases.Transfers.Commands.UnlinkTransfer;

/// <param name="OutCategoryId">Categoria de despesa que a ponta de saída passa a ter.</param>
/// <param name="InCategoryId">Categoria de receita que a ponta de entrada passa a ter.</param>
/// <param name="DeleteTransactions">Exclui as duas pontas em vez de devolvê-las como despesa/receita.</param>
public record UnlinkTransferCommand(
    Guid UserId,
    Guid LinkId,
    Guid? OutCategoryId,
    Guid? InCategoryId,
    bool DeleteTransactions
) : IRequest;
