using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Subcategories.Queries.GetSubcategoriesByCategory;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Subcategories;

public class GetSubcategoriesByCategoryQueryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ISubcategoryRepository> _subcategoryRepository = new();


    private GetSubcategoriesByCategoryQueryHandler CreateHandler() =>
        new(_categoryRepository.Object, _subcategoryRepository.Object);

    [Fact]
    public async Task Handle_DeveRetornarSubcategorias_QuandoCategoriaExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);
        var subcategories = new[]
        {
            SubcategoryBuilder.Build(categoryId: category.Id),
            SubcategoryBuilder.Build(categoryId: category.Id, name: "Mercado")
        };

        var query = new GetSubcategoriesByCategoryQuery(category.Id, userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _subcategoryRepository
            .Setup(r => r.GetByCategoryIdAsync(category.Id, default))
            .ReturnsAsync(subcategories);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoCategoriaNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var query = new GetSubcategoriesByCategoryQuery(categoryId, userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync((Domain.Entities.Category?)null);

        // Act
        var act = async () => await CreateHandler().Handle(query, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{categoryId}*");
    }
}
