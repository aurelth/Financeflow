using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.DeleteDefaultCategory;

public class DeleteDefaultCategoryCommandHandler(
    ICategoryRepository categoryRepository
) : IRequestHandler<DeleteDefaultCategoryCommand>
{
    public async Task Handle(
        DeleteDefaultCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetDefaultByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Categoria padrão não encontrada.");

        var hasTransactions = await categoryRepository.HasTransactionsAsync(
            request.Id, cancellationToken);

        if (hasTransactions)
            throw new ValidationException(
                "Não é possível excluir uma categoria padrão que possui transações vinculadas.",
                new Dictionary<string, string[]>
                {
                    { "Id", ["Não é possível excluir uma categoria padrão que possui transações vinculadas."] }
                });

        await categoryRepository.DeleteAsync(category, cancellationToken);
    }
}
