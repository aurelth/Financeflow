using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Subcategories.Commands.CreateSubcategory;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Subcategories;

public class CreateSubcategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ISubcategoryRepository> _subcategoryRepository = new();
    private readonly IMapper _mapper;

    public CreateSubcategoryCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CategoryMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private CreateSubcategoryCommandHandler CreateHandler() =>
        new(_categoryRepository.Object, _subcategoryRepository.Object, _mapper);

    [Fact]
    public async Task Handle_DeveCriarSubcategoria_QuandoDadosSaoValidos()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);

        var command = new CreateSubcategoryCommand(
            CategoryId: category.Id,
            UserId: userId,
            Name: "Mercado");

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _subcategoryRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, category.Id, default))
            .ReturnsAsync(false);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.IsActive.Should().BeTrue();

        _subcategoryRepository.Verify(r =>
            r.AddAsync(It.IsAny<Subcategory>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoCategoriaNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var command = new CreateSubcategoryCommand(
            CategoryId: categoryId,
            UserId: userId,
            Name: "Mercado");

        _categoryRepository
            .Setup(r => r.GetByIdAsync(categoryId, userId, default))
            .ReturnsAsync((Domain.Entities.Category?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*{categoryId}*");

        _subcategoryRepository.Verify(r =>
            r.AddAsync(It.IsAny<Subcategory>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoCategoriaEhPadrao()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.BuildDefault();

        var command = new CreateSubcategoryCommand(
            CategoryId: category.Id,
            UserId: userId,
            Name: "Mercado");

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*categorias padrão do sistema*");

        _subcategoryRepository.Verify(r =>
            r.AddAsync(It.IsAny<Subcategory>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoNomeJaExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);

        var command = new CreateSubcategoryCommand(
            CategoryId: category.Id,
            UserId: userId,
            Name: "Restaurante");

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        _subcategoryRepository
            .Setup(r => r.ExistsByNameAsync(command.Name, category.Id, default))
            .ReturnsAsync(true);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Já existe uma subcategoria com este nome*");

        _subcategoryRepository.Verify(r =>
            r.AddAsync(It.IsAny<Subcategory>(), default), Times.Never);
    }
}
