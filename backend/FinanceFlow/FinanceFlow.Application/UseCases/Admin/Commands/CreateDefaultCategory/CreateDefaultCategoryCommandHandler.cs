using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs.Admin;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.CreateDefaultCategory;

public class CreateDefaultCategoryCommandHandler(
    ICategoryRepository categoryRepository
) : IRequestHandler<CreateDefaultCategoryCommand, AdminCategoryDto>
{
    public async Task<AdminCategoryDto> Handle(
        CreateDefaultCategoryCommand request,
        CancellationToken cancellationToken)
    {
        var exists = await categoryRepository.DefaultExistsByNameAsync(
            request.Name, request.Type, null, cancellationToken);

        if (exists)
            throw new ValidationException(
                "Já existe uma categoria padrão com este nome.",
                new Dictionary<string, string[]>
                {
                    { "Name", ["Já existe uma categoria padrão com este nome."] }
                });

        var category = new Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Icon = request.Icon,
            Color = request.Color,
            Type = request.Type,
            IsDefault = true,
            IsActive = true,
            UserId = null,
            CreatedAt = DateTime.UtcNow,
        };

        await categoryRepository.AddAsync(category, cancellationToken);

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
