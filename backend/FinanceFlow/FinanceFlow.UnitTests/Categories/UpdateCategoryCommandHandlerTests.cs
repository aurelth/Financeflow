using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Categories.Commands.UpdateCategory;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.UnitTests.Common;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Categories;

public class UpdateCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly IMapper _mapper;

    public UpdateCategoryCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CategoryMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private UpdateCategoryCommandHandler CreateHandler() =>
        new(_categoryRepository.Object, _mapper);

    [Fact]
    public async Task Handle_DeveAtualizarCategoria_QuandoDadosSaoValidos()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var category = CategoryBuilder.Build(userId: userId);

        var command = new UpdateCategoryCommand(
            CategoryId: category.Id,
            UserId: userId,
            Name: "Novo Nome",
            Icon: "🚀",
            Color: "#22c55e");

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Novo Nome");
        result.Icon.Should().Be("🚀");
        result.Color.Should().Be("#22c55e");

        _categoryRepository.Verify(r =>
            r.UpdateAsync(category, default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoCategoriaNaoExiste()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var command = new UpdateCategoryCommand(
            CategoryId: categoryId,
            UserId: userId,
            Name: "Novo Nome",
            Icon: "🚀",
            Color: "#22c55e");

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

        var command = new UpdateCategoryCommand(
            CategoryId: category.Id,
            UserId: userId,
            Name: "Tentativa",
            Icon: "❌",
            Color: "#ff0000");

        _categoryRepository
            .Setup(r => r.GetByIdAsync(category.Id, userId, default))
            .ReturnsAsync(category);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*categorias padrão do sistema*");

        _categoryRepository.Verify(r =>
            r.UpdateAsync(category, default), Times.Never);
    }
}
