using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.Services;
using FinanceFlow.Application.UseCases.Transactions.Commands.CreateTransaction;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;

namespace FinanceFlow.UnitTests.Installments;

public class CreateInstallmentsTests
{
    private readonly Mock<ITransactionRepository> _transactions = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IEventPublisher> _events = new();
    private readonly Mock<IAttachmentService> _attachments = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ICacheService> _cache = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();

    private static readonly Category Expense = new()
    {
        Id = CategoryId, Name = "Eletrônicos", Icon = "x", Color = "#000", Type = TransactionType.Expense, UserId = UserId,
    };

    private List<Transaction> _saved = [];

    public CreateInstallmentsTests()
    {
        _configuration.Setup(c => c["Kafka:Topics:TransactionCreated"]).Returns("topic");
        _events.Setup(e => e.PublishAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _accounts.Setup(a => a.GetOrCreateDefaultAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Account.CreateDefault(UserId));
        _categories.Setup(c => c.GetByIdAsync(CategoryId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Expense);
        _cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _transactions
            .Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<Transaction>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<Transaction>, CancellationToken>((ts, _) => _saved = ts.ToList())
            .Returns(Task.CompletedTask);

        // Recarrega a primeira parcela como o repositório real faria (com a categoria)
        _transactions
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => { var first = _saved[0]; first.Category = Expense; return first; });
    }

    private CreateTransactionCommandHandler Handler() =>
        new(_transactions.Object, _categories.Object, _accounts.Object, _events.Object,
            _attachments.Object, _configuration.Object, new DashboardCacheInvalidator(_cache.Object));

    private static CreateTransactionCommand Command(
        decimal amount = 1200m, int? installments = 12, TransactionStatus status = TransactionStatus.Paid,
        DateTime? date = null, bool recurring = false) =>
        new(UserId, amount, TransactionType.Expense, date ?? new DateTime(2026, 11, 10), "Notebook",
            status, recurring, RecurrenceType.None, CategoryId, null, ["trabalho"],
            InstallmentCount: installments);

    [Fact]
    public async Task DeveCriarTodasAsParcelasDeUmaVez_ENuncaUmaPorUma()
    {
        var result = await Handler().Handle(Command(), default);

        _saved.Should().HaveCount(12);
        _transactions.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Transaction>>(), It.IsAny<CancellationToken>()), Times.Once);
        _transactions.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);

        result.InstallmentCount.Should().Be(12);
        result.InstallmentNumber.Should().Be(1);
    }

    [Fact]
    public async Task AsParcelasCompartilhamGrupo_ENumeracaoDeUmAN()
    {
        await Handler().Handle(Command(), default);

        _saved.Select(p => p.InstallmentGroupId).Distinct().Should().ContainSingle().Which.Should().NotBeNull();
        _saved.Select(p => p.InstallmentNumber).Should().Equal(Enumerable.Range(1, 12).Select(n => (int?)n));
        _saved.Should().OnlyContain(p => p.InstallmentCount == 12);
    }

    [Fact]
    public async Task ASomaDasParcelasEhOValorTotalInformado()
    {
        await Handler().Handle(Command(amount: 1000m, installments: 3), default);

        _saved.Select(p => p.Amount).Should().Equal(333.34m, 333.33m, 333.33m);
        _saved.Sum(p => p.Amount).Should().Be(1000m);
    }

    [Fact]
    public async Task ParcelasVencemUmMesDepoisDaOutra_ComDiaPreservado()
    {
        await Handler().Handle(Command(installments: 4, date: new DateTime(2026, 11, 30)), default);

        _saved.Select(p => p.Date).Should().Equal(
            new DateTime(2026, 11, 30), new DateTime(2026, 12, 30), new DateTime(2027, 1, 30), new DateTime(2027, 2, 28));
    }

    [Fact]
    public async Task PrimeiraParcelaTemOStatusEscolhido_AsDemaisFicamAgendadas()
    {
        await Handler().Handle(Command(status: TransactionStatus.Pending), default);

        _saved[0].Status.Should().Be(TransactionStatus.Pending);
        _saved.Skip(1).Should().OnlyContain(p => p.Status == TransactionStatus.Scheduled);
    }

    [Fact]
    public async Task ParcelasHerdamCategoriaContaTagsEDescricao_ENaoSaoRecorrentes()
    {
        await Handler().Handle(Command(installments: 3), default);

        _saved.Should().OnlyContain(p =>
            p.CategoryId == CategoryId && p.Description == "Notebook" && p.Tags == "[\"trabalho\"]" &&
            !p.IsRecurring && p.RecurrenceGroupId == null && p.RecurrenceType == RecurrenceType.None);
        _saved.Select(p => p.AccountId).Distinct().Should().ContainSingle();
    }

    [Fact]
    public async Task OAnexoFicaSoNaPrimeiraParcela()
    {
        _attachments
            .Setup(a => a.SaveAsync(It.IsAny<Stream>(), "nota.pdf", "application/pdf", UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(("path/nota.pdf", "nota.pdf"));

        var command = Command(installments: 3) with
        {
            AttachmentStream = new MemoryStream([1]), AttachmentFileName = "nota.pdf", AttachmentContentType = "application/pdf",
        };

        await Handler().Handle(command, default);

        _saved[0].AttachmentPath.Should().Be("path/nota.pdf");
        _saved.Skip(1).Should().OnlyContain(p => p.AttachmentPath == null);
    }

    [Fact]
    public async Task DeveInvalidarOCacheDoDashboardDeCadaMesDasParcelas()
    {
        await Handler().Handle(Command(installments: 3, date: new DateTime(2026, 11, 10)), default);

        foreach (var (year, month) in new[] { (2026, 11), (2026, 12), (2027, 1) })
            _cache.Verify(c => c.RemoveAsync($"dashboard:summary:{UserId}:{year}:{month}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DevePublicarOEventoSoParaAPrimeiraParcela_ComSeuValor()
    {
        await Handler().Handle(Command(amount: 1000m, installments: 3), default);

        _events.Verify(e => e.PublishAsync("topic", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LancamentoComumNaoVirouParcelado()
    {
        _transactions.Setup(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => _saved = [t]).Returns(Task.CompletedTask);

        var result = await Handler().Handle(Command(installments: null), default);

        _transactions.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Transaction>>(), It.IsAny<CancellationToken>()), Times.Never);
        _saved.Should().ContainSingle().Which.Amount.Should().Be(1200m);
        result.InstallmentGroupId.Should().BeNull();
        result.InstallmentNumber.Should().BeNull();
    }

    [Fact]
    public async Task DeveFalharSemGravarNada_QuandoACategoriaNaoCombinaComOTipo()
    {
        _categories.Setup(c => c.GetByIdAsync(CategoryId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = CategoryId, Name = "Salário", Icon = "x", Color = "#000", Type = TransactionType.Income, UserId = UserId });

        var act = () => Handler().Handle(Command(), default);

        await act.Should().ThrowAsync<ValidationException>();
        _transactions.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<Transaction>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Validador

    private static readonly CreateTransactionCommandValidator Validator = new();

    [Theory]
    [InlineData(2)]
    [InlineData(12)]
    [InlineData(60)]
    public void Validador_AceitaDe2a60Parcelas(int count) =>
        Validator.Validate(Command(installments: count)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(1)]
    [InlineData(0)]
    [InlineData(-3)]
    [InlineData(61)]
    public void Validador_RecusaNumeroDeParcelasForaDoIntervalo(int count) =>
        Validator.Validate(Command(installments: count)).Errors.Should().Contain(e => e.ErrorMessage.Contains("entre 2 e 60"));

    [Fact]
    public void Validador_NaoExigeParcelas_QuandoNulo() =>
        Validator.Validate(Command(installments: null)).IsValid.Should().BeTrue();

    [Fact]
    public void Validador_RecusaTransferenciaParcelada() =>
        Validator.Validate(Command() with { Type = TransactionType.Transfer })
            .Errors.Should().Contain(e => e.ErrorMessage.Contains("Transferências"));

    [Fact]
    public void Validador_RecusaParceladoERecorrenteAoMesmoTempo() =>
        Validator.Validate(Command(recurring: true))
            .Errors.Should().Contain(e => e.ErrorMessage.Contains("recorrente"));

    [Fact]
    public void Validador_RecusaTotalPequenoDemaisParaOsCentavosPorParcela() =>
        Validator.Validate(Command(amount: 0.05m, installments: 12))
            .Errors.Should().Contain(e => e.PropertyName == "Amount");

    [Fact]
    public void Validador_AceitaUmCentavoPorParcela() =>
        Validator.Validate(Command(amount: 0.12m, installments: 12)).IsValid.Should().BeTrue();
}
