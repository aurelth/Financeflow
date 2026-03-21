using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Subcategories.Commands.CreateSubcategory;

public class CreateSubcategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ISubcategoryRepository subcategoryRepository,
    IMapper mapper)
    : IRequestHandler<CreateSubcategoryCommand, SubcategoryDto>
{
    public async Task<SubcategoryDto> Handle(
        CreateSubcategoryCommand request,
        CancellationToken cancellationToken)
    {
        // Valida que a categoria existe e é acessível pelo utilizador
        var category = await categoryRepository
            .GetByIdAsync(request.CategoryId, request.UserId, cancellationToken);

        if (category is null)
            throw new NotFoundException("Categoria", request.CategoryId);

        // Não permite adicionar subcategorias a categorias padrão do sistema
        if (category.IsDefault == true)
            throw new ValidationException(
                "Não é possível adicionar subcategorias a categorias padrão do sistema.",
                new Dictionary<string, string[]>
                {
                    { "Category", ["Não é possível adicionar subcategorias a categorias padrão do sistema."] }
                });

        // Verifica nome duplicado na categoria
        var exists = await subcategoryRepository
            .ExistsByNameAsync(request.Name, request.CategoryId, cancellationToken);

        if (exists)
            throw new ValidationException(
                "Já existe uma subcategoria com este nome nesta categoria.",
                new Dictionary<string, string[]>
                {
                    { "Name", ["Já existe uma subcategoria com este nome nesta categoria."] }
                });

        var subcategory = new Subcategory
        {
            Id = Guid.NewGuid(),
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await subcategoryRepository.AddAsync(subcategory, cancellationToken);

        return mapper.Map<SubcategoryDto>(subcategory);
    }
}
