using AutoMapper;
using FinanceFlow.Application.Common.Mappings;
using FinanceFlow.Application.UseCases.Budgets.Queries.GetBudgets;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Budgets;

public class GetBudgetsQueryHandlerTests
{
    private readonly Mock<IBudgetRepository> _budgetRepository = new();
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

    public GetBudgetsQueryHandlerTests()
    {
        var config = new MapperConfiguration(cfg =>
            cfg.AddProfile<BudgetMappingProfile>());
        _mapper = config.CreateMapper();
    }

    private GetBudgetsQueryHandler CreateHandler() =>
        new(_budgetRepository.Object, _mapper);

    [Fact]
    public async Task Handle_DeveRetornarOrcamentos_QuandoExistemParaOPeriodo()
    {
        // Arrange
        var query = new GetBudgetsQuery(UserId, Month: 3, Year: 2026);

        var budgets = new List<Budget>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                CategoryId = CategoryId,
                Category = ValidCategory,
                Month = 3,
                Year = 2026,
                LimitAmount = 500.00m
            }
        };

        _budgetRepository
            .Setup(r => r.GetByUserAndPeriodAsync(UserId, 3, 2026, default))
            .ReturnsAsync(budgets);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result.First().LimitAmount.Should().Be(500.00m);
        result.First().CategoryName.Should().Be("Alimentação");
    }

    [Fact]
    public async Task Handle_DeveRetornarListaVazia_QuandoNaoExistemOrcamentos()
    {
        // Arrange
        var query = new GetBudgetsQuery(UserId, Month: 1, Year: 2026);

        _budgetRepository
            .Setup(r => r.GetByUserAndPeriodAsync(UserId, 1, 2026, default))
            .ReturnsAsync([]);

        // Act
        var result = await CreateHandler().Handle(query, default);

        // Assert
        result.Should().BeEmpty();
    }
}
