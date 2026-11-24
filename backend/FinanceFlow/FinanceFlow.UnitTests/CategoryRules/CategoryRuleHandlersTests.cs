using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.CategoryRules.Commands.DeleteCategoryRule;
using FinanceFlow.Application.UseCases.CategoryRules.Queries.GetCategoryRules;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.CategoryRules;

public class CategoryRuleHandlersTests
{
    private readonly Mock<ICategoryRuleRepository> _repository = new();
    private readonly Guid _userId = Guid.NewGuid();

    [Fact]
    public async Task Listar_DeveMapearRegraComCategoria()
    {
        var category = new Category { Id = Guid.NewGuid(), Name = "Alimentação", Icon = "🍽️", Color = "#ff0000", Type = TransactionType.Expense };
        var rule = CategoryRule.Create(_userId, TransactionType.Expense, "mercado extra", category.Id);
        rule.Category = category;
        rule.HitCount = 7;

        _repository
            .Setup(r => r.GetAllByUserAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([rule]);

        var result = await new GetCategoryRulesQueryHandler(_repository.Object)
            .Handle(new GetCategoryRulesQuery(_userId), default);

        var dto = result.Should().ContainSingle().Subject;
        dto.Key.Should().Be("mercado extra");
        dto.CategoryName.Should().Be("Alimentação");
        dto.CategoryIcon.Should().Be("🍽️");
        dto.HitCount.Should().Be(7);
    }

    [Fact]
    public async Task Excluir_DeveRemoverARegraEGravar()
    {
        var rule = CategoryRule.Create(_userId, TransactionType.Expense, "uber trip", Guid.NewGuid());
        _repository.Setup(r => r.GetByIdAsync(rule.Id, _userId, It.IsAny<CancellationToken>())).ReturnsAsync(rule);

        await new DeleteCategoryRuleCommandHandler(_repository.Object)
            .Handle(new DeleteCategoryRuleCommand(_userId, rule.Id), default);

        _repository.Verify(r => r.Remove(rule), Times.Once);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Excluir_DeveLancarNotFound_QuandoARegraNaoEDoUsuario()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CategoryRule?)null);

        var act = () => new DeleteCategoryRuleCommandHandler(_repository.Object)
            .Handle(new DeleteCategoryRuleCommand(_userId, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
        _repository.Verify(r => r.Remove(It.IsAny<CategoryRule>()), Times.Never);
    }
}
