using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Budgets.Commands.CreateBudget;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Budgets;

public class CreateBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly IMapper _mapper;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static readonly Category ValidCategory = new()
    {
        Id = CategoryId,
        Name = "Alimentação",
        Icon = "🍔",
        Color = "#f59e0b",
        Type = TransactionType.Expense,
        UserId = UserId
    };

    public CreateBudgetCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<BudgetMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private CreateBudgetCommandHandler CreateHandler() =>
        new(_budgetRepository.Object, _categoryRepository.Object, _mapper);

    [Fact]
    public async Task Handle_DeveCriarOrcamento_QuandoDadosSaoValidos()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            UserId: UserId,
            CategoryId: CategoryId,
            Month: 3,
            Year: 2026,
            LimitAmount: 500.00m);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        _budgetRepository
            .Setup(r => r.ExistsAsync(UserId, CategoryId, 3, 2026, default))
            .ReturnsAsync(false);

        _budgetRepository
            .Setup(r => r.AddAsync(It.IsAny<Budget>(), default))
            .Returns(Task.CompletedTask);

        var createdBudget = new Budget
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            CategoryId = CategoryId,
            Category = ValidCategory,
            Month = 3,
            Year = 2026,
            LimitAmount = 500.00m
        };

        _budgetRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), UserId, default))
            .ReturnsAsync(createdBudget);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.LimitAmount.Should().Be(500.00m);
        result.Month.Should().Be(3);
        result.Year.Should().Be(2026);
        result.CategoryId.Should().Be(CategoryId);

        _budgetRepository.Verify(r =>
            r.AddAsync(It.IsAny<Budget>(), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoCategoriaInexistente()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            UserId: UserId,
            CategoryId: Guid.NewGuid(),
            Month: 3,
            Year: 2026,
            LimitAmount: 500.00m);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(command.CategoryId, UserId, default))
            .ReturnsAsync((Category?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _budgetRepository.Verify(r =>
            r.AddAsync(It.IsAny<Budget>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveLancarValidationException_QuandoOrcamentoJaExiste()
    {
        // Arrange
        var command = new CreateBudgetCommand(
            UserId: UserId,
            CategoryId: CategoryId,
            Month: 3,
            Year: 2026,
            LimitAmount: 500.00m);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(CategoryId, UserId, default))
            .ReturnsAsync(ValidCategory);

        _budgetRepository
            .Setup(r => r.ExistsAsync(UserId, CategoryId, 3, 2026, default))
            .ReturnsAsync(true);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*já existe um orçamento*");

        _budgetRepository.Verify(r =>
            r.AddAsync(It.IsAny<Budget>(), default), Times.Never);
    }
}
