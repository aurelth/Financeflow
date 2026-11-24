using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Services;
using FinanceFlow.Application.UseCases.Transfers.Commands.CreateTransfer;
using FinanceFlow.Application.UseCases.Transfers.Commands.LinkTransfers;
using FinanceFlow.Application.UseCases.Transfers.Commands.UnlinkTransfer;
using FinanceFlow.Application.UseCases.Transfers.Queries.GetPendingTransfers;
using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Transfers;

public class TransferHandlersTests
{
    private readonly Mock<ITransferRepository> _transferRepository = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<IDashboardCacheInvalidator> _dashboardCacheInvalidator = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Account _accountA;
    private readonly Account _accountB;

    private static readonly DateTime Day = new(2026, 2, 12);

    public TransferHandlersTests()
    {
        _accountA = new Account { Id = Guid.NewGuid(), UserId = _userId, Name = "Conta A" };
        _accountB = new Account { Id = Guid.NewGuid(), UserId = _userId, Name = "Conta B" };

        foreach (var account in new[] { _accountA, _accountB })
        {
            var captured = account;
            _accountRepository
                .Setup(r => r.GetByIdAsync(captured.Id, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(captured);
        }
    }

    private Transaction Tx(
        Account account,
        TransactionType type,
        decimal amount = 100m,
        TransferDirection? direction = null,
        string description = "Lançamento",
        DateTime? date = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = _userId,
        AccountId = account.Id,
        Account = account,
        Amount = amount,
        Type = type,
        TransferDirection = direction,
        Description = description,
        Date = date ?? Day,
        CategoryId = type == TransactionType.Transfer ? WellKnownIds.TransferCategoryId : Guid.NewGuid(),
        SubcategoryId = type == TransactionType.Transfer ? null : Guid.NewGuid(),
    };

    // Criar transferência

    private CreateTransferCommandHandler CreateHandler() =>
        new(_accountRepository.Object, _transferRepository.Object);

    [Fact]
    public async Task Create_DeveGravarAsDuasPontasEOVinculoNumUnicoSave()
    {
        var result = await CreateHandler().Handle(
            new CreateTransferCommand(_userId, _accountA.Id, _accountB.Id, 250m, Day, null), default);

        result.Out.AccountId.Should().Be(_accountA.Id);
        result.Out.Direction.Should().Be(TransferDirection.Out);
        result.Out.Description.Should().Be("Transferência para Conta B");
        result.In.AccountId.Should().Be(_accountB.Id);
        result.In.Direction.Should().Be(TransferDirection.In);
        result.In.Description.Should().Be("Transferência de Conta A");
        result.Origin.Should().Be("Manual");

        _transferRepository.Verify(r => r.AddTransaction(It.Is<Transaction>(t =>
            t.Type == TransactionType.Transfer &&
            t.Amount == 250m &&
            t.CategoryId == WellKnownIds.TransferCategoryId &&
            t.Status == TransactionStatus.Paid)), Times.Exactly(2));
        _transferRepository.Verify(r => r.AddLink(It.Is<TransferLink>(l =>
            l.OutTransactionId == result.Out.TransactionId &&
            l.InTransactionId == result.In.TransactionId &&
            l.Origin == TransferLinkOrigin.Manual)), Times.Once);
        _transferRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_DeveUsarADescricaoInformadaNasDuasPontas()
    {
        var result = await CreateHandler().Handle(
            new CreateTransferCommand(_userId, _accountA.Id, _accountB.Id, 10m, Day, "  Reserva  "), default);

        result.Out.Description.Should().Be("Reserva");
        result.In.Description.Should().Be("Reserva");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Create_DeveRecusarValorNaoPositivo(decimal amount)
    {
        var act = () => CreateHandler().Handle(
            new CreateTransferCommand(_userId, _accountA.Id, _accountB.Id, amount, Day, null), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Create_DeveRecusarMesmaConta()
    {
        var act = () => CreateHandler().Handle(
            new CreateTransferCommand(_userId, _accountA.Id, _accountA.Id, 10m, Day, null), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Create_DeveRecusarContaArquivada()
    {
        _accountB.IsArchived = true;

        var act = () => CreateHandler().Handle(
            new CreateTransferCommand(_userId, _accountA.Id, _accountB.Id, 10m, Day, null), default);

        await act.Should().ThrowAsync<ValidationException>();
        _transferRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_DeveRetornarNaoEncontrado_QuandoContaNaoExiste()
    {
        var act = () => CreateHandler().Handle(
            new CreateTransferCommand(_userId, _accountA.Id, Guid.NewGuid(), 10m, Day, null), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Vincular lançamentos existentes

    private LinkTransfersCommandHandler LinkHandler() =>
        new(_transferRepository.Object, _dashboardCacheInvalidator.Object);

    private void SetupTracked(params Transaction[] transactions) =>
        _transferRepository
            .Setup(r => r.GetTrackedTransactionsAsync(
                _userId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

    [Fact]
    public async Task Link_DeveConverterDespesaEReceitaEmTransferencia_EVincular()
    {
        var expense = Tx(_accountA, TransactionType.Expense);
        var income = Tx(_accountB, TransactionType.Income);
        SetupTracked(expense, income);

        var result = await LinkHandler().Handle(
            new LinkTransfersCommand(_userId, income.Id, expense.Id), default);

        result.Out.TransactionId.Should().Be(expense.Id);
        result.In.TransactionId.Should().Be(income.Id);

        expense.Type.Should().Be(TransactionType.Transfer);
        expense.TransferDirection.Should().Be(TransferDirection.Out);
        expense.CategoryId.Should().Be(WellKnownIds.TransferCategoryId);
        expense.SubcategoryId.Should().BeNull();
        income.Type.Should().Be(TransactionType.Transfer);
        income.TransferDirection.Should().Be(TransferDirection.In);

        _transferRepository.Verify(r => r.AddLink(It.Is<TransferLink>(l =>
            l.OutTransactionId == expense.Id &&
            l.InTransactionId == income.Id &&
            l.Origin == TransferLinkOrigin.Manual)), Times.Once);
        _transferRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dashboardCacheInvalidator.Verify(c => c.InvalidateAsync(
            _userId, It.IsAny<IEnumerable<DateTime>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Link_DeveAceitarTransferenciasComSentidoJaDefinido()
    {
        var outTx = Tx(_accountA, TransactionType.Transfer, direction: TransferDirection.Out);
        var inTx = Tx(_accountB, TransactionType.Transfer, direction: TransferDirection.In);
        SetupTracked(outTx, inTx);

        var result = await LinkHandler().Handle(new LinkTransfersCommand(_userId, outTx.Id, inTx.Id), default);

        result.Out.TransactionId.Should().Be(outTx.Id);
        result.In.TransactionId.Should().Be(inTx.Id);
    }

    [Fact]
    public async Task Link_DeveRecusar_QuandoMesmaConta()
    {
        var a = Tx(_accountA, TransactionType.Expense);
        var b = Tx(_accountA, TransactionType.Income);
        SetupTracked(a, b);

        var act = () => LinkHandler().Handle(new LinkTransfersCommand(_userId, a.Id, b.Id), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Link_DeveRecusar_QuandoValoresDiferem()
    {
        var a = Tx(_accountA, TransactionType.Expense, 100m);
        var b = Tx(_accountB, TransactionType.Income, 99m);
        SetupTracked(a, b);

        var act = () => LinkHandler().Handle(new LinkTransfersCommand(_userId, a.Id, b.Id), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Link_DeveRecusar_QuandoOsDoisSaemOuOsDoisEntram()
    {
        var a = Tx(_accountA, TransactionType.Expense);
        var b = Tx(_accountB, TransactionType.Expense);
        SetupTracked(a, b);

        var act = () => LinkHandler().Handle(new LinkTransfersCommand(_userId, a.Id, b.Id), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Link_DeveRecusar_QuandoTransferenciaLegadaSemSentido()
    {
        var a = Tx(_accountA, TransactionType.Transfer, direction: null);
        var b = Tx(_accountB, TransactionType.Income);
        SetupTracked(a, b);

        var act = () => LinkHandler().Handle(new LinkTransfersCommand(_userId, a.Id, b.Id), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Link_DeveRecusar_QuandoUmDosLancamentosJaTemVinculo()
    {
        var a = Tx(_accountA, TransactionType.Expense);
        var b = Tx(_accountB, TransactionType.Income);
        SetupTracked(a, b);
        _transferRepository
            .Setup(r => r.IsLinkedAsync(b.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var act = () => LinkHandler().Handle(new LinkTransfersCommand(_userId, a.Id, b.Id), default);

        await act.Should().ThrowAsync<ValidationException>();
        _transferRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Link_DeveRetornarNaoEncontrado_QuandoUmLancamentoNaoExiste()
    {
        var a = Tx(_accountA, TransactionType.Expense);
        SetupTracked(a);

        var act = () => LinkHandler().Handle(
            new LinkTransfersCommand(_userId, a.Id, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Desfazer vínculo

    private UnlinkTransferCommandHandler UnlinkHandler() =>
        new(_transferRepository.Object, _categoryRepository.Object, _dashboardCacheInvalidator.Object);

    private (TransferLink Link, Transaction Out, Transaction In) SetupLink()
    {
        var outTx = Tx(_accountA, TransactionType.Transfer, direction: TransferDirection.Out);
        var inTx = Tx(_accountB, TransactionType.Transfer, direction: TransferDirection.In);
        var link = new TransferLink
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            OutTransactionId = outTx.Id,
            OutTransaction = outTx,
            InTransactionId = inTx.Id,
            InTransaction = inTx,
        };

        _transferRepository
            .Setup(r => r.GetLinkAsync(link.Id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(link);

        return (link, outTx, inTx);
    }

    private Guid SetupCategory(TransactionType type)
    {
        var id = Guid.NewGuid();
        _categoryRepository
            .Setup(r => r.GetByIdAsync(id, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = id, Type = type, UserId = _userId });
        return id;
    }

    [Fact]
    public async Task Unlink_DeveDevolverComoDespesaEReceita_ERejeitarOPar()
    {
        var (link, outTx, inTx) = SetupLink();
        var expenseCategory = SetupCategory(TransactionType.Expense);
        var incomeCategory = SetupCategory(TransactionType.Income);

        await UnlinkHandler().Handle(
            new UnlinkTransferCommand(_userId, link.Id, expenseCategory, incomeCategory, false), default);

        outTx.Type.Should().Be(TransactionType.Expense);
        outTx.TransferDirection.Should().BeNull();
        outTx.CategoryId.Should().Be(expenseCategory);
        inTx.Type.Should().Be(TransactionType.Income);
        inTx.TransferDirection.Should().BeNull();
        inTx.CategoryId.Should().Be(incomeCategory);

        _transferRepository.Verify(r => r.RemoveLink(link), Times.Once);
        _transferRepository.Verify(r => r.AddRejection(It.Is<TransferRejection>(x =>
            x.UserId == _userId &&
            new[] { x.FirstTransactionId, x.SecondTransactionId }.OrderBy(i => i)
                .SequenceEqual(new[] { outTx.Id, inTx.Id }.OrderBy(i => i)))), Times.Once);
        _transferRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unlink_DeveExcluirAsDuasPontas_QuandoPedido()
    {
        var (link, outTx, inTx) = SetupLink();

        await UnlinkHandler().Handle(
            new UnlinkTransferCommand(_userId, link.Id, null, null, true), default);

        outTx.DeletedAt.Should().NotBeNull();
        inTx.DeletedAt.Should().NotBeNull();
        _transferRepository.Verify(r => r.RemoveLink(link), Times.Once);
        _transferRepository.Verify(r => r.AddRejection(It.IsAny<TransferRejection>()), Times.Never);
    }

    [Fact]
    public async Task Unlink_DeveExigirAsDuasCategorias()
    {
        var (link, _, _) = SetupLink();
        var expenseCategory = SetupCategory(TransactionType.Expense);

        var act = () => UnlinkHandler().Handle(
            new UnlinkTransferCommand(_userId, link.Id, expenseCategory, null, false), default);

        await act.Should().ThrowAsync<ValidationException>();
        _transferRepository.Verify(r => r.RemoveLink(It.IsAny<TransferLink>()), Times.Never);
    }

    [Fact]
    public async Task Unlink_DeveRecusarCategoriaDoTipoErrado()
    {
        var (link, _, _) = SetupLink();
        var incomeCategoryUsedAsExpense = SetupCategory(TransactionType.Income);
        var incomeCategory = SetupCategory(TransactionType.Income);

        var act = () => UnlinkHandler().Handle(
            new UnlinkTransferCommand(_userId, link.Id, incomeCategoryUsedAsExpense, incomeCategory, false), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Unlink_DeveRetornarNaoEncontrado_QuandoVinculoNaoExiste()
    {
        var act = () => UnlinkHandler().Handle(
            new UnlinkTransferCommand(_userId, Guid.NewGuid(), null, null, true), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Pendências

    private GetPendingTransfersQueryHandler PendingHandler() => new(_transferRepository.Object);

    private void SetupPending(Transaction[] pending, Transaction[] candidates, params TransferRejection[] rejections)
    {
        _transferRepository
            .Setup(r => r.GetUnlinkedTransfersAsync(_userId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pending);
        _transferRepository
            .Setup(r => r.GetCandidatesAsync(
                _userId, null, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidates);
        _transferRepository
            .Setup(r => r.GetRejectionsAsync(
                _userId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(rejections);
    }

    [Fact]
    public async Task Pending_DeveRetornarVazio_QuandoNaoHaPontasSemPar()
    {
        SetupPending([], []);

        var result = await PendingHandler().Handle(new GetPendingTransfersQuery(_userId), default);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Pending_DeveSugerirOMelhorParDeOutraConta()
    {
        var pending = Tx(_accountA, TransactionType.Transfer, 400m, TransferDirection.Out, "PIX ENVIADO");
        var good = Tx(_accountB, TransactionType.Income, 400m, description: "PIX RECEBIDO");
        var sameAccount = Tx(_accountA, TransactionType.Income, 400m, description: "PIX RECEBIDO");
        var otherAmount = Tx(_accountB, TransactionType.Income, 399m, description: "PIX RECEBIDO");
        SetupPending([pending], [pending, good, sameAccount, otherAmount]);

        var result = await PendingHandler().Handle(new GetPendingTransfersQuery(_userId), default);

        var item = result.Should().ContainSingle().Subject;
        item.Suggestion.Should().NotBeNull();
        item.Suggestion!.Candidate.TransactionId.Should().Be(good.Id);
        item.Suggestion.WillConvert.Should().BeTrue();
        item.Suggestion.Score.Should().Be(80);
    }

    [Fact]
    public async Task Pending_NaoDeveSugerirParRejeitado()
    {
        var pending = Tx(_accountA, TransactionType.Transfer, 400m, TransferDirection.Out, "PIX ENVIADO");
        var candidate = Tx(_accountB, TransactionType.Income, 400m, description: "PIX RECEBIDO");
        SetupPending([pending], [candidate], TransferRejection.Create(_userId, candidate.Id, pending.Id));

        var result = await PendingHandler().Handle(new GetPendingTransfersQuery(_userId), default);

        result.Single().Suggestion.Should().BeNull();
    }

    [Fact]
    public async Task Pending_DeveListarPrimeiroQuemTemSugestao()
    {
        var withoutSuggestion = Tx(_accountA, TransactionType.Transfer, 10m, TransferDirection.Out,
            "TED", new DateTime(2026, 2, 20));
        var withSuggestion = Tx(_accountA, TransactionType.Transfer, 400m, TransferDirection.Out,
            "PIX ENVIADO", new DateTime(2026, 2, 1));
        var candidate = Tx(_accountB, TransactionType.Income, 400m, description: "PIX RECEBIDO",
            date: new DateTime(2026, 2, 1));
        SetupPending([withoutSuggestion, withSuggestion], [candidate]);

        var result = await PendingHandler().Handle(new GetPendingTransfersQuery(_userId), default);

        result.Select(r => r.Transaction.TransactionId)
            .Should().Equal(withSuggestion.Id, withoutSuggestion.Id);
    }
}
