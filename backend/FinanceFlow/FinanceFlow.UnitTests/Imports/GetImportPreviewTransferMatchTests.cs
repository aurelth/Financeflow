using FinanceFlow.Application.UseCases.Imports.Queries.GetImportPreview;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Imports;

public class GetImportPreviewTransferMatchTests
{
    private readonly Mock<IBankImportRepository> _bankImportRepository = new();
    private readonly Mock<ITransferRepository> _transferRepository = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _importId = Guid.NewGuid();

    private GetImportPreviewQueryHandler CreateHandler() =>
        new(_bankImportRepository.Object, _transferRepository.Object);

    private (BankImportTransaction Row, Transaction Candidate) Setup(
        TransferMatchStatus status = TransferMatchStatus.Auto,
        TransactionType candidateType = TransactionType.Income,
        bool deleted = false)
    {
        var accountB = new Account { Id = Guid.NewGuid(), Name = "Conta B" };
        var candidate = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            AccountId = accountB.Id,
            Account = accountB,
            Amount = 1000m,
            Type = candidateType,
            Date = new DateTime(2026, 2, 12),
            Description = "PIX RECEBIDO",
            DeletedAt = deleted ? DateTime.UtcNow : null,
        };

        var row = new BankImportTransaction
        {
            Id = Guid.NewGuid(),
            Amount = 1000m,
            Description = "PIX ENVIADO",
            Type = TransactionType.Expense,
            Date = new DateTime(2026, 2, 12),
            TransferMatchTransactionId = candidate.Id,
            TransferMatchStatus = status,
            TransferMatchScore = 80,
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(_importId, _userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankImport
            {
                Id = _importId,
                UserId = _userId,
                Status = BankImportStatus.Completed,
                Transactions = [row],
            });

        _bankImportRepository
            .Setup(r => r.GetTransactionsByIdsAsync(
                _userId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([candidate]);

        return (row, candidate);
    }

    [Fact]
    public async Task Handle_DeveExporOParDeTransferencia_ESeusTotais()
    {
        var (_, candidate) = Setup(TransferMatchStatus.Auto, TransactionType.Income);

        var result = await CreateHandler().Handle(new GetImportPreviewQuery(_importId, _userId), default);

        var match = result.Transactions.Single().TransferMatch;
        match.Should().NotBeNull();
        match!.TransactionId.Should().Be(candidate.Id);
        match.AccountName.Should().Be("Conta B");
        match.Status.Should().Be("Auto");
        match.Score.Should().Be(80);
        match.WillConvert.Should().BeTrue();
        result.AutoTransfers.Should().Be(1);
        result.SuggestedTransfers.Should().Be(0);
    }

    [Fact]
    public async Task Handle_NaoDeveConverter_QuandoOutraPontaJaEhTransferencia()
    {
        Setup(TransferMatchStatus.Suggested, TransactionType.Transfer);

        var result = await CreateHandler().Handle(new GetImportPreviewQuery(_importId, _userId), default);

        result.Transactions.Single().TransferMatch!.WillConvert.Should().BeFalse();
        result.SuggestedTransfers.Should().Be(1);
    }

    [Fact]
    public async Task Handle_DeveOcultarOPar_QuandoOutraPontaFoiVinculadaDepois()
    {
        var (_, candidate) = Setup();
        _transferRepository
            .Setup(r => r.IsLinkedAsync(candidate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().Handle(new GetImportPreviewQuery(_importId, _userId), default);

        result.Transactions.Single().TransferMatch.Should().BeNull();
        result.AutoTransfers.Should().Be(0);
    }

    [Fact]
    public async Task Handle_DeveOcultarOPar_QuandoOutraPontaFoiExcluida()
    {
        Setup(deleted: true);

        var result = await CreateHandler().Handle(new GetImportPreviewQuery(_importId, _userId), default);

        result.Transactions.Single().TransferMatch.Should().BeNull();
    }
}
