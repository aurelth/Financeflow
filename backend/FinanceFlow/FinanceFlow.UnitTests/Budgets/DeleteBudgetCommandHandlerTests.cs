using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Budgets.Commands.DeleteBudget;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Budgets;

public class DeleteBudgetCommandHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepository = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid BudgetId = Guid.NewGuid();

    private DeleteBudgetCommandHandler CreateHandler() =>
        new(_budgetRepository.Object);

    [Fact]
    public async Task Handle_DeveEliminarOrcamento_QuandoExiste()
    {
        // Arrange
        var command = new DeleteBudgetCommand(BudgetId, UserId);

        var budget = new Budget
        {
            Id = BudgetId,
            UserId = UserId,
            CategoryId = Guid.NewGuid(),
            Month = 3,
            Year = 2026,
            LimitAmount = 500.00m
        };

        _budgetRepository
            .Setup(r => r.GetByIdAsync(BudgetId, UserId, default))
            .ReturnsAsync(budget);

        _budgetRepository
            .Setup(r => r.DeleteAsync(budget, default))
            .Returns(Task.CompletedTask);

        // Act
        await CreateHandler().Handle(command, default);

        // Assert
        _budgetRepository.Verify(r =>
            r.DeleteAsync(budget, default), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveLancarNotFoundException_QuandoOrcamentoNaoExiste()
    {
        // Arrange
        var command = new DeleteBudgetCommand(Guid.NewGuid(), UserId);

        _budgetRepository
            .Setup(r => r.GetByIdAsync(command.Id, UserId, default))
            .ReturnsAsync((Budget?)null);

        // Act
        var act = async () => await CreateHandler().Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _budgetRepository.Verify(r =>
            r.DeleteAsync(It.IsAny<Budget>(), default), Times.Never);
    }
}
