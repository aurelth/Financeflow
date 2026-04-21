using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Goals;
using FinanceFlow.Application.UseCases.Goals.Commands.CreateGoal;
using FinanceFlow.Application.UseCases.Goals.Commands.DeleteGoal;
using FinanceFlow.Application.UseCases.Goals.Commands.UpdateGoal;
using FinanceFlow.Application.UseCases.Goals.Queries.GetGoalsSummary;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Goals;

public class GoalHandlerTests
{
    private readonly Mock<IGoalRepository> _goalRepository = new();
    private readonly Mock<IGoalProgressService> _goalProgressService = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid GoalId = Guid.NewGuid();

    private static readonly GoalsSummaryResultDto ValidSummary = new(
        AvailableThisMonth: 1500,
        CommittedThisMonth: 900,
        Difference: 600,
        Goals:
        [
            new GoalProgressResultDto(
                Id:                  GoalId,
                Name:                "Viagem",
                Emoji:               "✈️",
                TargetAmount:        5000,
                MonthlyContribution: 400,
                Deadline:            DateTime.UtcNow.AddMonths(12),
                AccumulatedAmount:   800,
                PlannedThisMonth:    400,
                ReceivedThisMonth:   400,
                ProgressPercentage:  16,
                IsCompleted:         false,
                MonthsToComplete:    11,
                Status:              "OnTrack"),
        ]);

    private static readonly Goal ValidGoal = new()
    {
        Id = GoalId,
        UserId = UserId,
        Name = "Viagem",
        Emoji = "✈️",
        TargetAmount = 5000,
        MonthlyContribution = 400,
        Deadline = DateTime.UtcNow.AddMonths(12),
        CreatedAt = DateTime.UtcNow,
    };

    // GetGoalsSummaryQueryHandler

    [Fact]
    public async Task GetGoalsSummary_DeveRetornarResultado_QuandoServicoFunciona()
    {
        // Arrange
        _goalProgressService
            .Setup(s => s.CalculateAsync(UserId, default))
            .ReturnsAsync(ValidSummary);

        var handler = new GetGoalsSummaryQueryHandler(_goalProgressService.Object);
        var query = new GetGoalsSummaryQuery(UserId);

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Goals.Should().HaveCount(1);
        result.AvailableThisMonth.Should().Be(1500);
        result.CommittedThisMonth.Should().Be(900);
        result.Difference.Should().Be(600);

        _goalProgressService.Verify(s =>
            s.CalculateAsync(UserId, default), Times.Once);
    }

    // CreateGoalCommandHandler

    [Fact]
    public async Task CreateGoal_DeveCriarMeta_QuandoDadosSaoValidos()
    {
        // Arrange
        Guid capturedGoalId = Guid.Empty;

        _goalRepository
            .Setup(r => r.AddAsync(It.IsAny<Goal>(), default))
            .Callback<Goal, CancellationToken>((g, _) => capturedGoalId = g.Id)
            .Returns(Task.CompletedTask);

        _goalProgressService
            .Setup(s => s.CalculateAsync(UserId, default))
            .ReturnsAsync(() => new GoalsSummaryResultDto(
                AvailableThisMonth: 1500,
                CommittedThisMonth: 500,
                Difference: 1000,
                Goals:
                [
                    new GoalProgressResultDto(
                    Id:                  capturedGoalId,
                    Name:                "Viagem",
                    Emoji:               "✈️",
                    TargetAmount:        5000,
                    MonthlyContribution: 400,
                    Deadline:            DateTime.UtcNow.AddMonths(12),
                    AccumulatedAmount:   0,
                    PlannedThisMonth:    400,
                    ReceivedThisMonth:   400,
                    ProgressPercentage:  0,
                    IsCompleted:         false,
                    MonthsToComplete:    13,
                    Status:              "OnTrack"),
                ]));

        var handler = new CreateGoalCommandHandler(
            _goalRepository.Object,
            _goalProgressService.Object);

        var command = new CreateGoalCommand(
            UserId: UserId,
            Name: "Viagem",
            TargetAmount: 5000,
            MonthlyContribution: 400,
            Deadline: DateTime.UtcNow.AddMonths(12),
            Emoji: "✈️");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Viagem");

        _goalRepository.Verify(r =>
            r.AddAsync(It.IsAny<Goal>(), default), Times.Once);
    }

    // UpdateGoalCommandHandler

    [Fact]
    public async Task UpdateGoal_DeveAtualizar_QuandoMetaExiste()
    {
        // Arrange
        _goalRepository
            .Setup(r => r.GetByIdAsync(GoalId, UserId, default))
            .ReturnsAsync(ValidGoal);

        _goalRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Goal>(), default))
            .Returns(Task.CompletedTask);

        _goalProgressService
            .Setup(s => s.CalculateAsync(UserId, default))
            .ReturnsAsync(ValidSummary);

        var handler = new UpdateGoalCommandHandler(
            _goalRepository.Object,
            _goalProgressService.Object);

        var command = new UpdateGoalCommand(
            Id: GoalId,
            UserId: UserId,
            Name: "Viagem Europa",
            TargetAmount: 8000,
            MonthlyContribution: 500,
            Deadline: DateTime.UtcNow.AddMonths(18),
            Emoji: "🌍");

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Should().NotBeNull();

        _goalRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Goal>(), default), Times.Once);
    }

    [Fact]
    public async Task UpdateGoal_DeveLancarNotFoundException_QuandoMetaNaoExiste()
    {
        // Arrange
        _goalRepository
            .Setup(r => r.GetByIdAsync(GoalId, UserId, default))
            .ReturnsAsync((Goal?)null);

        var handler = new UpdateGoalCommandHandler(
            _goalRepository.Object,
            _goalProgressService.Object);

        var command = new UpdateGoalCommand(
            Id: GoalId,
            UserId: UserId,
            Name: "Viagem",
            TargetAmount: 5000,
            MonthlyContribution: 400,
            Deadline: DateTime.UtcNow.AddMonths(12),
            Emoji: "✈️");

        // Act
        var act = async () => await handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _goalRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<Goal>(), default), Times.Never);
    }

    // DeleteGoalCommandHandler

    [Fact]
    public async Task DeleteGoal_DeveRemover_QuandoMetaExiste()
    {
        // Arrange
        _goalRepository
            .Setup(r => r.GetByIdAsync(GoalId, UserId, default))
            .ReturnsAsync(ValidGoal);

        _goalRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Goal>(), default))
            .Returns(Task.CompletedTask);

        var handler = new DeleteGoalCommandHandler(_goalRepository.Object);
        var command = new DeleteGoalCommand(GoalId, UserId);

        // Act
        await handler.Handle(command, default);

        // Assert
        _goalRepository.Verify(r =>
            r.DeleteAsync(It.IsAny<Goal>(), default), Times.Once);
    }

    [Fact]
    public async Task DeleteGoal_DeveLancarNotFoundException_QuandoMetaNaoExiste()
    {
        // Arrange
        _goalRepository
            .Setup(r => r.GetByIdAsync(GoalId, UserId, default))
            .ReturnsAsync((Goal?)null);

        var handler = new DeleteGoalCommandHandler(_goalRepository.Object);
        var command = new DeleteGoalCommand(GoalId, UserId);

        // Act
        var act = async () => await handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();

        _goalRepository.Verify(r =>
            r.DeleteAsync(It.IsAny<Goal>(), default), Times.Never);
    }
}
