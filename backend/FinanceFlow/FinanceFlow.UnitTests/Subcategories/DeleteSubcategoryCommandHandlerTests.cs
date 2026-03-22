using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Subcategories.Commands.DeleteSubcategory;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Subcategories;

public class DeleteSubcategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ISubcategoryRepository> _subcategoryRepository = new();

    private DeleteSubcategoryCommandHandler CreateHandler() =>
        new(_categoryRepository.Object, _subcategoryRepository.Object);

    [Fact]
    public async Task Handle_DeveFazerSoftDelete_QuandoTemTransacoes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);
        var subcategory = SubcategoryBuilder.Build(categoryId: category.Id);

        var command = new DeleteSubcategoryCommand(
            SubcategoryId: subcategory.Id,
            CategoryId: category.Id,
            UserId: userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _subcategoryRepository
            .Setup(r => r.GetByIdAsync(subcategory.Id, category.Id, default))
            .ReturnsAsync(subcategory);

        _subcategoryRepository
            .Setup(r => r.HasTransactionsAsync(subcategory.Id, default))
            .ReturnsAsync(true);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        subcategory.DeletedAt.Should().NotBeNull();
        subcategory.IsActive.Should().BeFalse();

        _subcategoryRepository.Verify(r =>
            r.UpdateAsync(subcategory, default), Times.Once);
        _subcategoryRepository.Verify(r =>
            r.DeleteAsync(It.IsAny<Subcategory>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveFazerDeleteFisico_QuandoNaoTemTransacoes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);
        var subcategory = SubcategoryBuilder.Build(categoryId: category.Id);

        var command = new DeleteSubcategoryCommand(
            SubcategoryId: subcategory.Id,
            CategoryId: category.Id,
            UserId: userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _subcategoryRepository
            .Setup(r => r.GetByIdAsync(subcategory.Id, category.Id, default))
            .ReturnsAsync(subcategory);

        _subcategoryRepository
            .Setup(r => r.HasTransactionsAsync(subcategory.Id, default))
            .ReturnsAsync(false);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _subcategoryRepository.Verify(r =>
            r.DeleteAsync(subcategory, default), Times.Once);
        _subcategoryRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Subcategory>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoCategoriaNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var command = new DeleteSubcategoryCommand(
            SubcategoryId: Guid.NewGuid(),
            CategoryId: categoryId,
            UserId: userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync((Domain.Entities.Category?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{categoryId}*");
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoSubcategoriaNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);
        var subcategoryId = Guid.NewGuid();

        var command = new DeleteSubcategoryCommand(
            SubcategoryId: subcategoryId,
            CategoryId: category.Id,
            UserId: userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _subcategoryRepository
            .Setup(r => r.GetByIdAsync(subcategoryId, category.Id, default))
            .ReturnsAsync((Domain.Entities.Subcategory?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{subcategoryId}*");
    }
}
