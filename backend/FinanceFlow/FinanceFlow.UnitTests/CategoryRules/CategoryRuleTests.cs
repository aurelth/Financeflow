using FinanceFlow.Domain.Entities;
using FluentAssertions;

namespace FinanceFlow.UnitTests.CategoryRules;

public class CategoryRuleTests
{
    [Fact]
    public void Create_DeveIniciarComUmaConfirmacao()
    {
        var userId = Guid.NewGuid();
        var categoryId = Guid.NewGuid();

        var rule = CategoryRule.Create(userId, TransactionType.Expense, "mercado extra", categoryId);

        rule.UserId.Should().Be(userId);
        rule.Key.Should().Be("mercado extra");
        rule.Type.Should().Be(TransactionType.Expense);
        rule.CategoryId.Should().Be(categoryId);
        rule.HitCount.Should().Be(1);
    }

    [Fact]
    public void Reinforce_DeveSomarConfirmacoes_QuandoACategoriaEAMesma()
    {
        var categoryId = Guid.NewGuid();
        var rule = CategoryRule.Create(Guid.NewGuid(), TransactionType.Expense, "uber trip", categoryId);

        rule.Reinforce(categoryId, 3);

        rule.CategoryId.Should().Be(categoryId);
        rule.HitCount.Should().Be(4);
    }

    [Fact]
    public void Reinforce_DeveTrocarACategoriaERecomecarAContagem_QuandoOUsuarioMuda()
    {
        var rule = CategoryRule.Create(Guid.NewGuid(), TransactionType.Expense, "uber trip", Guid.NewGuid());
        rule.Reinforce(rule.CategoryId, 4);
        var newCategory = Guid.NewGuid();

        rule.Reinforce(newCategory);

        rule.CategoryId.Should().Be(newCategory);
        rule.HitCount.Should().Be(1);
    }
}
