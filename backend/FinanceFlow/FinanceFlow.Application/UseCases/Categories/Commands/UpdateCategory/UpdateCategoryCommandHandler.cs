using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Commands.UpdateCategory;

public class UpdateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IMapper mapper)
    : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(
        UpdateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository
            .GetByIdAsync(request.CategoryId, request.UserId, cancellationToken);

        if (category is null)
            throw new NotFoundException("Categoria", request.CategoryId);

        // Não permite editar categorias padrão do sistema
        if (category.IsDefault == true)
            throw new ValidationException(
                "Não é possível editar categorias padrão do sistema.",
                new Dictionary<string, string[]>
                {
                    { "Category", ["Não é possível editar categorias padrão do sistema."] }
                });

        category.Name = request.Name.Trim();
        category.Icon = request.Icon.Trim();
        category.Color = request.Color.Trim();
        category.UpdatedAt = DateTime.UtcNow;

        await categoryRepository.UpdateAsync(category, cancellationToken);

        return mapper.Map<CategoryDto>(category);
    }
}
