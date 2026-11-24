using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Services.Ai;
using FinanceFlow.Domain.Entities;
using FluentAssertions;

namespace FinanceFlow.UnitTests.QuickEntry;

public class QuickEntryReplyParserTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);

    private readonly Category _alimentacao = new() { Id = Guid.NewGuid(), Name = "Alimentação", Type = TransactionType.Expense };
    private readonly Category _salario = new() { Id = Guid.NewGuid(), Name = "Salário", Type = TransactionType.Income };

    private IReadOnlyCollection<Category> Categories => [_alimentacao, _salario];

    private IReadOnlyList<ParsedEntry> Parse(string reply, int max = 5) =>
        QuickEntryReplyParser.Parse(reply, Today, Categories, max);

    [Fact]
    public void Parse_DeveLerRespostaLimpa()
    {
        var reply = $$"""
            {"entries":[{"type":"Expense","amount":45.9,"date":"2026-11-23","description":"Almoço iFood","categoryId":"{{_alimentacao.Id}}"}]}
            """;

        var entry = Parse(reply).Should().ContainSingle().Subject;

        entry.Type.Should().Be(TransactionType.Expense);
        entry.Amount.Should().Be(45.9m);
        entry.Date.Should().Be(new DateOnly(2026, 11, 23));
        entry.Description.Should().Be("Almoço iFood");
        entry.CategoryId.Should().Be(_alimentacao.Id);
    }

    [Fact]
    public void Parse_DeveAceitarCercaDeCodigoETextoEmVolta()
    {
        var reply = """
            Claro! Aqui está:
            ```json
            {"entries":[{"type":"expense","amount":10,"date":"2026-11-24","description":"Café","categoryId":null}]}
            ```
            """;

        Parse(reply).Should().ContainSingle().Which.Description.Should().Be("Café");
    }

    [Theory]
    [InlineData("sem json nenhum")]
    [InlineData("{ isto não é json }")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"outra\":1}")]
    [InlineData("{\"entries\":\"não é lista\"}")]
    public void Parse_DeveLancarValidacao_QuandoARespostaNaoEntendivel(string reply)
    {
        var act = () => Parse(reply);

        act.Should().Throw<ValidationException>().WithMessage(QuickEntryReplyParser.UnreadableMessage);
    }

    [Fact]
    public void Parse_DeveRetornarVazio_QuandoNaoHaLancamentos() =>
        Parse("{\"entries\":[]}").Should().BeEmpty();

    [Theory]
    [InlineData("\"45,90\"", 45.90)]
    [InlineData("\"R$ 45.90\"", 45.90)]
    [InlineData("12.345", 12.35)]
    public void Parse_DeveNormalizarOValor(string amount, double expected)
    {
        var reply = $$"""{"entries":[{"type":"Expense","amount":{{amount}},"date":"2026-11-24","description":"X"}]}""";

        Parse(reply).Single().Amount.Should().Be((decimal)expected);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-10")]
    [InlineData("\"abc\"")]
    [InlineData("null")]
    [InlineData("1000000000")]
    public void Parse_DeveDescartarLancamentoSemValorValido(string amount)
    {
        var reply = $$"""{"entries":[{"type":"Expense","amount":{{amount}},"date":"2026-11-24","description":"X"}]}""";

        Parse(reply).Should().BeEmpty();
    }

    [Theory]
    [InlineData("Transfer")]
    [InlineData("")]
    [InlineData("gasto")]
    public void Parse_DeveDescartarLancamentoSemTipoValido(string type)
    {
        var reply = $$"""{"entries":[{"type":"{{type}}","amount":10,"date":"2026-11-24","description":"X"}]}""";

        Parse(reply).Should().BeEmpty();
    }

    [Theory]
    [InlineData("24/11/2026")]
    [InlineData("ontem")]
    [InlineData("1999-01-01")]   // longe demais no passado
    [InlineData("2031-01-01")]   // longe demais no futuro
    [InlineData("")]
    public void Parse_DeveUsarHoje_QuandoADataENaoPlausivel(string date)
    {
        var reply = $$"""{"entries":[{"type":"Expense","amount":10,"date":"{{date}}","description":"X"}]}""";

        Parse(reply).Single().Date.Should().Be(Today);
    }

    [Fact]
    public void Parse_DeveAceitarDataFuturaPlausivel_ParaContasAPagar()
    {
        var reply = """{"entries":[{"type":"Expense","amount":10,"date":"2026-12-05","description":"Boleto"}]}""";

        Parse(reply).Single().Date.Should().Be(new DateOnly(2026, 12, 5));
    }

    [Fact]
    public void Parse_DeveIgnorarCategoriaInventada()
    {
        var reply = $$"""{"entries":[{"type":"Expense","amount":10,"date":"2026-11-24","description":"X","categoryId":"{{Guid.NewGuid()}}"}]}""";

        Parse(reply).Single().CategoryId.Should().BeNull();
    }

    [Fact]
    public void Parse_DeveIgnorarCategoriaDeOutroTipo()
    {
        // Salário é de receita, o lançamento é despesa
        var reply = $$"""{"entries":[{"type":"Expense","amount":10,"date":"2026-11-24","description":"X","categoryId":"{{_salario.Id}}"}]}""";

        Parse(reply).Single().CategoryId.Should().BeNull();
    }

    [Fact]
    public void Parse_DeveIgnorarCategoriaMalFormada()
    {
        var reply = """{"entries":[{"type":"Expense","amount":10,"date":"2026-11-24","description":"X","categoryId":"não-é-guid"}]}""";

        Parse(reply).Single().CategoryId.Should().BeNull();
    }

    [Fact]
    public void Parse_DeveLimparEAbreviarADescricao()
    {
        var long_ = new string('a', 300);
        var reply = $$"""{"entries":[{"type":"Expense","amount":10,"date":"2026-11-24","description":"  Posto\n\tShell   \u0007 BR  "},{"type":"Expense","amount":10,"date":"2026-11-24","description":"{{long_}}"}]}""";

        var entries = Parse(reply);

        entries[0].Description.Should().Be("Posto Shell BR");
        entries[1].Description.Length.Should().Be(QuickEntryReplyParser.MaxDescriptionLength);
    }

    [Fact]
    public void Parse_DeveDarDescricaoPadrao_QuandoVazia()
    {
        var reply = """{"entries":[{"type":"Expense","amount":10,"date":"2026-11-24","description":"   "}]}""";

        Parse(reply).Single().Description.Should().Be("Lançamento por IA");
    }

    [Fact]
    public void Parse_DeveRespeitarOMaximoDeLancamentos()
    {
        var one = """{"type":"Expense","amount":10,"date":"2026-11-24","description":"X"}""";
        var reply = $"{{\"entries\":[{string.Join(",", Enumerable.Repeat(one, 9))}]}}";

        Parse(reply, max: 3).Should().HaveCount(3);
    }

    [Fact]
    public void Parse_DevePularItensInvalidosSemPerderOsValidos()
    {
        var reply = """{"entries":[42,"x",{"type":"Expense","amount":5,"date":"2026-11-24","description":"Bom"}]}""";

        Parse(reply).Should().ContainSingle().Which.Description.Should().Be("Bom");
    }
}
