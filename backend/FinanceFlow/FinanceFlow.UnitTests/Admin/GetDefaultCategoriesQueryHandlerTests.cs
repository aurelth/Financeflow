using FinanceFlow.Application.UseCases.Admin.Queries.GetDefaultCategories;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Admin;

public class GetDefaultCategoriesQueryHandlerTests
{
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly GetDefaultCategoriesQueryHandler _handler;

    public GetDefaultCategoriesQueryHandlerTests()
    {
        _handler = new GetDefaultCategoriesQueryHandler(_categoryRepository.Object);
    }

    [Fact]
    public async Task Handle_DeveRetornarCategoriasPadrao_QuandoExistem()
    {
        // Arrange
        var categories = new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Alimentação", Icon = "utensils", Color = "#f97316", Type = TransactionType.Expense, IsDefault = true },
            new() { Id = Guid.NewGuid(), Name = "Salário",     Icon = "briefcase", Color = "#22c55e", Type = TransactionType.Income,  IsDefault = true },
        };

        _categoryRepository.Setup(r => r.GetAllDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(categories);

        var query = new GetDefaultCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(c => c.Name == "Alimentação");
        result.Should().Contain(c => c.Name == "Salário");
    }

    [Fact]
    public async Task Handle_DeveRetornarListaVazia_QuandoNaoExistem()
    {
        // Arrange
        _categoryRepository.Setup(r => r.GetAllDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var query = new GetDefaultCategoriesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }
}
