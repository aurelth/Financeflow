using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Admin;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.UpdateDefaultCategory;

public class UpdateDefaultCategoryCommandHandler(
    ICategoryRepository categoryRepository
) : IRequestHandler<UpdateDefaultCategoryCommand, AdminCategoryDto>
{
    public async Task<AdminCategoryDto> Handle(
        UpdateDefaultCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var category = await categoryRepository.GetDefaultByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Categoria padrão não encontrada.");

        var nameChanged = !string.Equals(
            category.Name, request.Name.Trim(),
            StringComparison.OrdinalIgnoreCase);

        if (nameChanged)
        {
            var exists = await categoryRepository.DefaultExistsByNameAsync(
                request.Name, category.Type, request.Id, cancellationToken);

            if (exists)
                throw new ValidationException(
                    "Já existe uma categoria padrão com este nome.",
                    new Dictionary<string, string[]>
                    {
                        { "Name", ["Já existe uma categoria padrão com este nome."] }
                    });
        }

        category.Name = request.Name.Trim();
        category.Icon = request.Icon;
        category.Color = request.Color;

        await categoryRepository.UpdateAsync(category, cancellationToken);

        return new AdminCategoryDto(
            Id: category.Id,
            Name: category.Name,
            Icon: category.Icon,
            Color: category.Color,
            Type: category.Type,
            IsActive: category.IsActive,
            CreatedAt: category.CreatedAt
        );
    }
}
