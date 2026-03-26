using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Categories.Commands.CreateCategory;

public class CreateCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IMapper mapper)
    : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    public async Task<CategoryDto> Handle(
        CreateCategoryCommand request,
        CancellationToken cancellationToken)
    {
        // Verifica se já existe categoria com o mesmo nome e tipo
        var exists = await categoryRepository.ExistsByNameAsync(
            request.Name, request.UserId, request.Type, cancellationToken);

        if (exists)
            throw new ValidationException(
                "Já existe uma categoria com este nome.",
                new Dictionary<string, string[]>
                {
                    { "Name", ["Já existe uma categoria com este nome."] }
                });

        var category = new Category
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Name = request.Name.Trim(),
            Icon = request.Icon.Trim(),
            Color = request.Color.Trim(),
            Type = request.Type,
            IsDefault = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await categoryRepository.AddAsync(category, cancellationToken);

        return mapper.Map<CategoryDto>(category);
    }
}
