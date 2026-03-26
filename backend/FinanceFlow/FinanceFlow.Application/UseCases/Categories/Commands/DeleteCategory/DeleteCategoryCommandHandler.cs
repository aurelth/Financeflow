using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Commands.DeleteCategory;

public class DeleteCategoryCommandHandler(
    ICategoryRepository categoryRepository)
    : IRequestHandler<DeleteCategoryCommand>
{
    public async Task Handle(
        DeleteCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository
            .GetByIdAsync(request.CategoryId, request.UserId, cancellationToken);

        if (category is null)
            throw new NotFoundException("Categoria", request.CategoryId);

        // Não permite deletar categorias padrão do sistema
        if (category.IsDefault == true)
            throw new ValidationException(
                "Não é possível remover categorias padrão do sistema.",
                new Dictionary<string, string[]>
                {
                    { "Category", ["Não é possível remover categorias padrão do sistema."] }
                });

        // Verifica se tem transações — se sim, apenas soft delete
        var hasTransactions = await categoryRepository
            .HasTransactionsAsync(request.CategoryId, cancellationToken);

        if (hasTransactions)
        {
            // Soft delete — preserva o histórico financeiro
            category.DeletedAt = DateTime.UtcNow;
            category.IsActive = false;
            await categoryRepository.UpdateAsync(category, cancellationToken);
        }
        else
        {
            // Remoção física — sem transações vinculadas
            await categoryRepository.DeleteAsync(category, cancellationToken);
        }
    }
}
