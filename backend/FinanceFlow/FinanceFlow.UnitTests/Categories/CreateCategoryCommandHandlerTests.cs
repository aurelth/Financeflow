using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Categories.Commands.CreateCategory;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Categories;

public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly IMapper _mapper;

    public CreateCategoryCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<CategoryMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private CreateCategoryCommandHandler CreateHandler() =>
        new(_categoryRepository.Object, _mapper);

    [Fact]
    public async Task Handle_DeveCriarCategoria_QuandoDadosSaoValidos()
    {
        // Arrange
        var command = new CreateCategoryCommand(
            UserId: Guid.NewGuid(),
            Name: "Lazer",
            Icon: "🎮",
            Color: "#6366f1",
            Type: TransactionType.Expense);

        _categoryRepository
            .Setup(r => r.ExistsByNameAsync(
                command.Name, command.UserId, command.Type, default))
            .ReturnsAsync(false);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.Icon.Should().Be(command.Icon);
        result.Color.Should().Be(command.Color);
        result.IsOwner.Should().BeTrue();
        result.IsDefault.Should().BeFalse();

        _categoryRepository.Verify(r =>
            r.AddAsync(It.IsAny<Category>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoNomeJaExiste()
    {
        // Arrange
        var command = new CreateCategoryCommand(
            UserId: Guid.NewGuid(),
            Name: "Alimentação",
            Icon: "🍔",
            Color: "#6366f1",
            Type: TransactionType.Expense);

        _categoryRepository
            .Setup(r => r.ExistsByNameAsync(
                command.Name, command.UserId, command.Type, default))
            .ReturnsAsync(true);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Já existe uma categoria com este nome*");

        _categoryRepository.Verify(r =>
            r.AddAsync(It.IsAny<Category>(), default), Times.Never);
    }
}
