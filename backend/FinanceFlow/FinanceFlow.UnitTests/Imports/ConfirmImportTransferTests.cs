using FinanceFlow.Application.DTOs.Imports;
using FinanceFlow.Application.Services;
using FinanceFlow.Application.UseCases.Imports.Commands.ConfirmImport;
using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FinanceFlow.UnitTests.Imports;

/// <summary>Confirmação da importação quando a linha do extrato tem um par de transferência em outra conta.</summary>
public class ConfirmImportTransferTests
{
    private readonly Mock<IBankImportRepository> _bankImportRepository = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICategoryRepository> _categoryRepository = new();
    private readonly Mock<ITransferRepository> _transferRepository = new();
    private readonly Mock<ICategoryRuleRepository> _categoryRuleRepository = new();
    private readonly Mock<IDashboardCacheInvalidator> _dashboardCacheInvalidator = new();

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _importId = Guid.NewGuid();
    private readonly Guid _accountA = Guid.NewGuid();
    private readonly Guid _accountB = Guid.NewGuid();
    private readonly Guid _expenseCategoryId = Guid.NewGuid();

    private ConfirmImportCommandHandler CreateHandler() =>
        new(_bankImportRepository.Object,
            _transactionRepository.Object,
            _categoryRepository.Object,
            _transferRepository.Object,
            _categoryRuleRepository.Object,
            _dashboardCacheInvalidator.Object,
            NullLogger<ConfirmImportCommandHandler>.Instance);

    // Linha do extrato da conta A (saída de 1000) com par na conta B
    private (BankImportTransaction Row, Transaction Candidate) Setup(
        TransferMatchStatus status,
        TransactionType candidateType = TransactionType.Income,
        TransferDirection? candidateDirection = null,
        Guid? candidateAccountId = null,
        bool candidateLinked = false,
        bool candidateMissing = false)
    {
        var candidate = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            AccountId = candidateAccountId ?? _accountB,
            Amount = 1000m,
            Type = candidateType,
            TransferDirection = candidateDirection,
            Date = new DateTime(2026, 2, 12),
            Description = "PIX RECEBIDO",
            CategoryId = Guid.NewGuid(),
            SubcategoryId = Guid.NewGuid(),
        };

        var row = new BankImportTransaction
        {
            Id = Guid.NewGuid(),
            Hash = "hash-pix",
            Amount = 1000m,
            Description = "PIX ENVIADO",
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 2, 12),
            IsSelected = true,
            TransferMatchTransactionId = candidate.Id,
            TransferMatchStatus = status,
            TransferMatchScore = status == TransferMatchStatus.Auto ? 80 : 60,
        };

        var bankImport = new BankImport
        {
            Id = _importId,
            UserId = _userId,
            AccountId = _accountA,
            Status = BankImportStatus.Completed,
            Transactions = [row],
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(_importId, _userId, default))
            .ReturnsAsync(bankImport);

        _transferRepository
            .Setup(r => r.GetTrackedTransactionsAsync(
                _userId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidateMissing ? [] : [candidate]);

        _transferRepository
            .Setup(r => r.IsLinkedAsync(candidate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidateLinked);

        _categoryRepository
            .Setup(r => r.GetByIdAsync(_expenseCategoryId, _userId, default))
            .ReturnsAsync(new Category { Id = _expenseCategoryId, Type = TransactionType.Expense });

        return (row, candidate);
    }

    private async Task<BankImportDto> ConfirmAsync(
        BankImportTransaction row, string type, string? action, Guid? categoryId = null) =>
        await CreateHandler().Handle(
            new ConfirmImportCommand(_importId, _userId, new ConfirmImportRequestDto([
                new ConfirmImportItemDto(row.Id, true, categoryId ?? Guid.Empty, type, TransferAction: action)
            ])),
            default);

    [Fact]
    public async Task Handle_DeveVincularAsPontas_QuandoParAutomaticoSemAcaoExplicita()
    {
        var (row, candidate) = Setup(TransferMatchStatus.Auto);

        var result = await ConfirmAsync(row, "Expense", action: null, _expenseCategoryId);

        result.Imported.Should().Be(1);

        _transferRepository.Verify(r => r.AddTransaction(It.Is<Transaction>(t =>
            t.AccountId == _accountA &&
            t.Type == TransactionType.Transfer &&
            t.TransferDirection == TransferDirection.Out &&
            t.CategoryId == WellKnownIds.TransferCategoryId &&
            t.Amount == 1000m)), Times.Once);

        _transferRepository.Verify(r => r.AddLink(It.Is<TransferLink>(l =>
            l.UserId == _userId &&
            l.InTransactionId == candidate.Id &&
            l.Origin == TransferLinkOrigin.Auto &&
            l.Score == 80)), Times.Once);

        _transferRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionRepository.Verify(r => r.AddAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveConverterAReceitaExistenteEmEntradaDeTransferencia()
    {
        var (row, candidate) = Setup(TransferMatchStatus.Auto, TransactionType.Income);

        await ConfirmAsync(row, "Transfer", TransferActions.Link);

        candidate.Type.Should().Be(TransactionType.Transfer);
        candidate.TransferDirection.Should().Be(TransferDirection.In);
        candidate.CategoryId.Should().Be(WellKnownIds.TransferCategoryId);
        candidate.SubcategoryId.Should().BeNull();

        _dashboardCacheInvalidator.Verify(c => c.InvalidateAsync(
            _userId,
            It.Is<IEnumerable<DateTime>>(d => d.Contains(candidate.Date)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDeveMexerNaOutraPonta_QuandoJaEhTransferenciaPendente()
    {
        var (row, candidate) = Setup(
            TransferMatchStatus.Auto, TransactionType.Transfer, TransferDirection.In);
        var originalCategory = candidate.CategoryId;

        await ConfirmAsync(row, "Transfer", TransferActions.Link);

        candidate.Type.Should().Be(TransactionType.Transfer);
        candidate.TransferDirection.Should().Be(TransferDirection.In);
        candidate.CategoryId.Should().Be(originalCategory);
        _transferRepository.Verify(r => r.AddLink(It.IsAny<TransferLink>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDeveVincular_QuandoSugestaoSemAcaoExplicita()
    {
        var (row, _) = Setup(TransferMatchStatus.Suggested);

        var result = await ConfirmAsync(row, "Expense", action: null, _expenseCategoryId);

        result.Imported.Should().Be(1);
        _transferRepository.Verify(r => r.AddLink(It.IsAny<TransferLink>()), Times.Never);
        _transferRepository.Verify(r => r.AddRejection(It.IsAny<TransferRejection>()), Times.Never,
            "o usuário não decidiu: a sugestão continua valendo nas pendências");
        _transactionRepository.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.Type == TransactionType.Expense && t.CategoryId == _expenseCategoryId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveVincular_QuandoUsuarioAceitaASugestao()
    {
        var (row, candidate) = Setup(TransferMatchStatus.Suggested);

        await ConfirmAsync(row, "Expense", TransferActions.Link);

        _transferRepository.Verify(r => r.AddLink(It.Is<TransferLink>(l =>
            l.InTransactionId == candidate.Id &&
            l.Origin == TransferLinkOrigin.Suggested &&
            l.Score == 60)), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveImportarComoLancamentoComum_ERegistrarARejeicao_QuandoUsuarioIgnoraOPar()
    {
        var (row, candidate) = Setup(TransferMatchStatus.Auto);

        var result = await ConfirmAsync(row, "Expense", TransferActions.Ignore, _expenseCategoryId);

        result.Imported.Should().Be(1);
        _transferRepository.Verify(r => r.AddLink(It.IsAny<TransferLink>()), Times.Never);
        _transactionRepository.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.Type == TransactionType.Expense && t.TransferDirection == null),
            It.IsAny<CancellationToken>()), Times.Once);

        _transferRepository.Verify(r => r.AddRejection(It.Is<TransferRejection>(x =>
            x.UserId == _userId &&
            new[] { x.FirstTransactionId, x.SecondTransactionId }.Contains(candidate.Id))), Times.Once);
        _transferRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveImportarComoTransferenciaSemPar_QuandoVinculoExplicitoNaoEhMaisValido()
    {
        // A outra ponta foi vinculada a outra transferência entre o preview e a confirmação
        var (row, _) = Setup(TransferMatchStatus.Auto, candidateLinked: true);

        var result = await ConfirmAsync(row, "Transfer", TransferActions.Link);

        result.Imported.Should().Be(1);
        _transferRepository.Verify(r => r.AddLink(It.IsAny<TransferLink>()), Times.Never);
        _transactionRepository.Verify(r => r.AddAsync(
            It.Is<Transaction>(t =>
                t.Type == TransactionType.Transfer &&
                t.TransferDirection == TransferDirection.Out &&
                t.CategoryId == WellKnownIds.TransferCategoryId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_DeveVoltarALancamentoComum_QuandoVinculoAutomaticoDeixouDeValer()
    {
        var (row, _) = Setup(TransferMatchStatus.Auto, candidateMissing: true);

        var result = await ConfirmAsync(row, "Expense", action: null, _expenseCategoryId);

        result.Imported.Should().Be(1);
        _transferRepository.Verify(r => r.AddLink(It.IsAny<TransferLink>()), Times.Never);
        _transactionRepository.Verify(r => r.AddAsync(
            It.Is<Transaction>(t => t.Type == TransactionType.Expense),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDeveVincular_QuandoOutraPontaEstaNaMesmaConta()
    {
        var (row, _) = Setup(TransferMatchStatus.Auto, candidateAccountId: _accountA);

        await ConfirmAsync(row, "Expense", action: null, _expenseCategoryId);

        _transferRepository.Verify(r => r.AddLink(It.IsAny<TransferLink>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveUsarOSentidoDoExtrato_QuandoMarcadaComoTransferenciaSemPar()
    {
        var (row, _) = Setup(TransferMatchStatus.Suggested);
        row.TransferMatchTransactionId = null;
        row.TransferMatchStatus = null;
        row.Type = TransactionType.Income;

        await ConfirmAsync(row, "Transfer", action: null);

        _transactionRepository.Verify(r => r.AddAsync(
            It.Is<Transaction>(t =>
                t.Type == TransactionType.Transfer &&
                t.TransferDirection == TransferDirection.In),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
