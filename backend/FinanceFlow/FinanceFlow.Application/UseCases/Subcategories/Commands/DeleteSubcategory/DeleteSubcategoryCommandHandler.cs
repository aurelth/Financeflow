using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Subcategories.Commands.DeleteSubcategory;

public class DeleteSubcategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ISubcategoryRepository subcategoryRepository)
    : IRequestHandler<DeleteSubcategoryCommand>
{
    public async Task Handle(
        DeleteSubcategoryCommand request,
        CancellationToken cancellationToken)
    {
        // Valida que a categoria existe e é acessível pelo utilizador
        var category = await categoryRepository
            .GetByIdAsync(request.CategoryId, request.UserId, cancellationToken);

        if (category is null)
            throw new NotFoundException("Categoria", request.CategoryId);

        // Busca a subcategoria
        var subcategory = await subcategoryRepository
            .GetByIdAsync(request.SubcategoryId, request.CategoryId, cancellationToken);

        if (subcategory is null)
            throw new NotFoundException("Subcategoria", request.SubcategoryId);

        // Verifica se tem transações — se sim, soft delete
        var hasTransactions = await subcategoryRepository
            .HasTransactionsAsync(request.SubcategoryId, cancellationToken);

        if (hasTransactions)
        {
            subcategory.DeletedAt = DateTime.UtcNow;
            subcategory.IsActive = false;
            await subcategoryRepository.UpdateAsync(subcategory, cancellationToken);
        }
        else
        {
            await subcategoryRepository.DeleteAsync(subcategory, cancellationToken);
        }
    }
}
