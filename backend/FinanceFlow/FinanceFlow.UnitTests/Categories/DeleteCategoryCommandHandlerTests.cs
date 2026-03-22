using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Categories.Commands.DeleteCategory;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Categories;

public class DeleteCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();

    private DeleteCategoryCommandHandler CreateHandler() =>
        new(_categoryRepository.Object);

    [Fact]
    public async Task Handle_DeveFazerSoftDelete_QuandoTemTransacoes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);
        var command = new DeleteCategoryCommand(category.Id, userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _categoryRepository
            .Setup(r => r.HasTransactionsAsync(category.Id, default))
            .ReturnsAsync(true);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        category.DeletedAt.Should().NotBeNull();
        category.IsActive.Should().BeFalse();

        _categoryRepository.Verify(r =>
            r.UpdateAsync(category, default), Times.Once);
        _categoryRepository.Verify(r =>
            r.DeleteAsync(It.IsAny<Category>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveFazerDeleteFisico_QuandoNaoTemTransacoes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);
        var command = new DeleteCategoryCommand(category.Id, userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _categoryRepository
            .Setup(r => r.HasTransactionsAsync(category.Id, default))
            .ReturnsAsync(false);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _categoryRepository.Verify(r =>
            r.DeleteAsync(category, default), Times.Once);
        _categoryRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Category>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoCategoriaNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();
        var command = new DeleteCategoryCommand(categoryId, userId);

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
    public async Task Handle_DeveLancarValidationException_QuandoCategoriaEhPadrao()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.BuildDefault();
        var command = new DeleteCategoryCommand(category.Id, userId);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*categorias padrão do sistema*");

        _categoryRepository.Verify(r =>
            r.DeleteAsync(It.IsAny<Category>(), default), Times.Never);
        _categoryRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Category>(), default), Times.Never);
    }
}
