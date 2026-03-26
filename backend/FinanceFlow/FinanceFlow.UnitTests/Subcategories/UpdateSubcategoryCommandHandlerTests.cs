using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Subcategories.Commands.UpdateSubcategory;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Subcategories;

public class UpdateSubcategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ISubcategoryRepository> _subcategoryRepository = new();
    private readonly IMapper _mapper;

    public UpdateSubcategoryCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CategoryMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private UpdateSubcategoryCommandHandler CreateHandler() =>
        new(_categoryRepository.Object, _subcategoryRepository.Object, _mapper);

    [Fact]
    public async Task Handle_DeveAtualizarSubcategoria_QuandoDadosSaoValidos()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);
        var subcategory = SubcategoryBuilder.Build(categoryId: category.Id);

        var command = new UpdateSubcategoryCommand(
            SubcategoryId: subcategory.Id,
            CategoryId: category.Id,
            UserId: userId,
            Name: "Novo Nome");

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _subcategoryRepository
            .Setup(r => r.GetByIdAsync(subcategory.Id, category.Id, default))
            .ReturnsAsync(subcategory);

        _subcategoryRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, category.Id, default))
            .ReturnsAsync(false);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Novo Nome");

        _subcategoryRepository.Verify(r =>
            r.UpdateAsync(subcategory, default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveAtualizarSubcategoria_QuandoNomeEhOMesmo()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);
        var subcategory = SubcategoryBuilder.Build(categoryId: category.Id, name: "Restaurante");

        var command = new UpdateSubcategoryCommand(
            SubcategoryId: subcategory.Id,
            CategoryId: category.Id,
            UserId: userId,
            Name: "Restaurante"); // mesmo nome — não deve lançar exceção

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _subcategoryRepository
            .Setup(r => r.GetByIdAsync(subcategory.Id, category.Id, default))
            .ReturnsAsync(subcategory);

        _subcategoryRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, category.Id, default))
            .ReturnsAsync(true); // existe, mas é o próprio

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        _subcategoryRepository.Verify(r =>
            r.UpdateAsync(subcategory, default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoCategoriaNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var command = new UpdateSubcategoryCommand(
            SubcategoryId: Guid.NewGuid(),
            CategoryId: categoryId,
            UserId: userId,
            Name: "Novo Nome");

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

        var command = new UpdateSubcategoryCommand(
            SubcategoryId: subcategoryId,
            CategoryId: category.Id,
            UserId: userId,
            Name: "Novo Nome");

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

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoNomeDuplicado()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);
        var subcategory = SubcategoryBuilder.Build(categoryId: category.Id, name: "Restaurante");

        var command = new UpdateSubcategoryCommand(
            SubcategoryId: subcategory.Id,
            CategoryId: category.Id,
            UserId: userId,
            Name: "Mercado"); // nome diferente que já existe

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _subcategoryRepository
            .Setup(r => r.GetByIdAsync(subcategory.Id, category.Id, default))
            .ReturnsAsync(subcategory);

        _subcategoryRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, category.Id, default))
            .ReturnsAsync(true);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Já existe uma subcategoria com este nome*");

        _subcategoryRepository.Verify(r =>
            r.UpdateAsync(subcategory, default), Times.Never);
    }
}
