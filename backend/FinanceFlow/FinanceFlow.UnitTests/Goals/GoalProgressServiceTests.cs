using FinanceFlow.Application.Services;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FinanceFlow.UnitTests.Goals;

public class GoalProgressServiceTests
{
    private readonly Mock<IGoalRepository> _goalRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ILogger<GoalProgressService>> _logger = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    private GoalProgressService CreateService() =>
        new(_goalRepository.Object, _transactionRepository.Object, _logger.Object);

    private static Goal CreateGoal(Guid? linkedCategoryId = null, decimal target = 5000, decimal monthly = 500) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Name = "Viagem",
            Emoji = "✈️",
            TargetAmount = target,
            MonthlyContribution = monthly,
            Deadline = DateTime.UtcNow.AddMonths(12),
            LinkedCategoryId = linkedCategoryId,
            CreatedAt = DateTime.UtcNow,
        };

    // Testes para metas com LinkedCategoryId

    [Fact]
    public async Task Calculate_DeveRetornarProgressoZero_QuandoNaoHaTransacoes()
    {
        // Arrange
        var goal = CreateGoal(linkedCategoryId: CategoryId);

        _goalRepository
            .Setup(r => r.GetByUserAsync(UserId, default))
            .ReturnsAsync([goal]);

        _transactionRepository
            .Setup(r => r.GetTotalByCategoryAsync(CategoryId, default))
            .ReturnsAsync(0m);

        _transactionRepository
            .Setup(r => r.GetTotalByCategoryAndMonthAsync(
                CategoryId,
                It.IsAny<int>(),
                It.IsAny<int>(),
                default))
            .ReturnsAsync(0m);

        var service = CreateService();

        // Act
        var result = await service.CalculateAsync(UserId, default);

        // Assert
        result.Goals.Should().HaveCount(1);
        var goalResult = result.Goals.First();
        goalResult.AccumulatedAmount.Should().Be(0);
        goalResult.ReceivedThisMonth.Should().Be(0);
        goalResult.ProgressPercentage.Should().Be(0);
    }

    [Fact]
    public async Task Calculate_DeveRetornarProgressoCorreto_QuandoHaTransacoes()
    {
        // Arrange
        var goal = CreateGoal(linkedCategoryId: CategoryId, target: 5000, monthly: 500);

        _goalRepository
            .Setup(r => r.GetByUserAsync(UserId, default))
            .ReturnsAsync([goal]);

        _transactionRepository
            .Setup(r => r.GetTotalByCategoryAsync(CategoryId, default))
            .ReturnsAsync(1000m);

        _transactionRepository
            .Setup(r => r.GetTotalByCategoryAndMonthAsync(
                CategoryId,
                It.IsAny<int>(),
                It.IsAny<int>(),
                default))
            .ReturnsAsync(300m);

        var service = CreateService();

        // Act
        var result = await service.CalculateAsync(UserId, default);

        // Assert
        var goalResult = result.Goals.First();
        goalResult.AccumulatedAmount.Should().Be(1000);
        goalResult.ReceivedThisMonth.Should().Be(300);
        goalResult.PlannedThisMonth.Should().Be(500);
        goalResult.ProgressPercentage.Should().Be(20);
        goalResult.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Calculate_DeveMarcarComoConcluida_QuandoAccumuladoAtingeAlvo()
    {
        // Arrange
        var goal = CreateGoal(linkedCategoryId: CategoryId, target: 5000, monthly: 500);

        _goalRepository
            .Setup(r => r.GetByUserAsync(UserId, default))
            .ReturnsAsync([goal]);

        _transactionRepository
            .Setup(r => r.GetTotalByCategoryAsync(CategoryId, default))
            .ReturnsAsync(5000m);

        _transactionRepository
            .Setup(r => r.GetTotalByCategoryAndMonthAsync(
                CategoryId,
                It.IsAny<int>(),
                It.IsAny<int>(),
                default))
            .ReturnsAsync(0m);

        var service = CreateService();

        // Act
        var result = await service.CalculateAsync(UserId, default);

        // Assert
        var goalResult = result.Goals.First();
        goalResult.AccumulatedAmount.Should().Be(5000);
        goalResult.IsCompleted.Should().BeTrue();
        goalResult.ProgressPercentage.Should().Be(100);
        goalResult.PlannedThisMonth.Should().Be(0);
        goalResult.ReceivedThisMonth.Should().Be(0);
        goalResult.Status.Should().Be("Completed");
    }

    [Fact]
    public async Task Calculate_DeveRetornarListaVazia_QuandoNaoHaMetas()
    {
        // Arrange
        _goalRepository
            .Setup(r => r.GetByUserAsync(UserId, default))
            .ReturnsAsync([]);

        var service = CreateService();

        // Act
        var result = await service.CalculateAsync(UserId, default);

        // Assert
        result.Goals.Should().BeEmpty();
        result.AvailableThisMonth.Should().Be(0);
        result.CommittedThisMonth.Should().Be(0);
        result.Difference.Should().Be(0);
    }

    [Fact]
    public async Task Calculate_DeveCalcularMonthsToComplete_Corretamente()
    {
        // Arrange
        var goal = CreateGoal(linkedCategoryId: CategoryId, target: 5000, monthly: 500);

        _goalRepository
            .Setup(r => r.GetByUserAsync(UserId, default))
            .ReturnsAsync([goal]);

        _transactionRepository
            .Setup(r => r.GetTotalByCategoryAsync(CategoryId, default))
            .ReturnsAsync(1000m);

        _transactionRepository
            .Setup(r => r.GetTotalByCategoryAndMonthAsync(
                CategoryId,
                It.IsAny<int>(),
                It.IsAny<int>(),
                default))
            .ReturnsAsync(0m);

        var service = CreateService();

        // Act
        var result = await service.CalculateAsync(UserId, default);

        // Assert
        var goalResult = result.Goals.First();
        // Restante = 4000, contribuição = 500 → 8 meses
        goalResult.MonthsToComplete.Should().Be(8);
    }

    [Fact]
    public async Task Calculate_DeveRetornarStatusOverdue_QuandoPrazoVencido()
    {
        // Arrange
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Name = "Meta Vencida",
            Emoji = "⏰",
            TargetAmount = 5000,
            MonthlyContribution = 500,
            Deadline = DateTime.UtcNow.AddMonths(-1), // Prazo vencido
            LinkedCategoryId = CategoryId,
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
        };

        _goalRepository
            .Setup(r => r.GetByUserAsync(UserId, default))
            .ReturnsAsync([goal]);

        _transactionRepository
            .Setup(r => r.GetTotalByCategoryAsync(CategoryId, default))
            .ReturnsAsync(1000m);

        _transactionRepository
            .Setup(r => r.GetTotalByCategoryAndMonthAsync(
                CategoryId,
                It.IsAny<int>(),
                It.IsAny<int>(),
                default))
            .ReturnsAsync(0m);

        var service = CreateService();

        // Act
        var result = await service.CalculateAsync(UserId, default);

        // Assert
        result.Goals.First().Status.Should().Be("Overdue");
    }
}
