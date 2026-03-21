using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Subcategories.Commands.UpdateSubcategory;

public class UpdateSubcategoryCommandHandler(
    ICategoryRepository categoryRepository,
    ISubcategoryRepository subcategoryRepository,
    IMapper mapper)
    : IRequestHandler<UpdateSubcategoryCommand, SubcategoryDto>
{
    public async Task<SubcategoryDto> Handle(
        UpdateSubcategoryCommand request,
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

        // Verifica nome duplicado (exceto o próprio)
        var exists = await subcategoryRepository
            .ExistsByNameAsync(request.Name, request.CategoryId, cancellationToken);

        if (exists && subcategory.Name != request.Name.Trim())
            throw new ValidationException(
                "Já existe uma subcategoria com este nome nesta categoria.",
                new Dictionary<string, string[]>
                {
                    { "Name", ["Já existe uma subcategoria com este nome nesta categoria."] }
                });

        subcategory.Name = request.Name.Trim();
        subcategory.UpdatedAt = DateTime.UtcNow;

        await subcategoryRepository.UpdateAsync(subcategory, cancellationToken);

        return mapper.Map<SubcategoryDto>(subcategory);
    }
}
