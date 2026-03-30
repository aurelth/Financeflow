using AutoMapper;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Budgets.Commands.UpdateBudget;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Budgets;

public class UpdateBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepository = new();
    private readonly IMapper _mapper;

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid BudgetId = Guid.NewGuid();
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

    public UpdateBudgetCommandHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<BudgetMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private UpdateBudgetCommandHandler CreateHandler() =>
        new(_budgetRepository.Object, _mapper);

    [Fact]
    public async Task Handle_DeveAtualizarLimite_QuandoDadosSaoValidos()
    {
        // Arrange
        var command = new UpdateBudgetCommand(
            Id: BudgetId,
            UserId: UserId,
            LimitAmount: 800.00m);

        var existingBudget = new Budget
        {
            Id = BudgetId,
            UserId = UserId,
            CategoryId = CategoryId,
            Category = ValidCategory,
            Month = 3,
            Year = 2026,
            LimitAmount = 500.00m
        };

        _budgetRepository
            .Setup(r => r.GetByIdAsync(BudgetId, UserId, default))
            .ReturnsAsync(existingBudget);

        _budgetRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Budget>(), default))
            .Returns(Task.CompletedTask);

        var updatedBudget = new Budget
        {
            Id = BudgetId,
            UserId = UserId,
            CategoryId = CategoryId,
            Category = ValidCategory,
            Month = 3,
            Year = 2026,
            LimitAmount = 800.00m  // valor atualizado
        };

        _budgetRepository
            .SetupSequence(r => r.GetByIdAsync(BudgetId, UserId, default))
            .ReturnsAsync(existingBudget)
            .ReturnsAsync(updatedBudget);

        // Act
        var result = await CreateHandler().Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.LimitAmount.Should().Be(800.00m);

        _budgetRepository.Verify(r =>
            r.UpdateAsync(It.Is<Budget>(b => b.LimitAmount == 800.00m), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoOrcamentoNaoExiste()
    {
        // Arrange
        var command = new UpdateBudgetCommand(
            Id: Guid.NewGuid(),
            UserId: UserId,
            LimitAmount: 800.00m);

        _budgetRepository
            .Setup(r => r.GetByIdAsync(command.Id, UserId, default))
            .ReturnsAsync((Budget?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _budgetRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Budget>(), default), Times.Never);
    }
}
