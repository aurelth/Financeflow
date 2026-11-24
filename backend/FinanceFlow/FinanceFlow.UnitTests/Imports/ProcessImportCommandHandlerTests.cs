using FinanceFlow.Application.Services;
using FinanceFlow.Application.UseCases.Imports.Commands.ProcessImport;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Imports;

public class ProcessImportCommandHandlerTests
{
    private readonly Mock<IBankImportRepository> _bankImportRepository = new();
    private readonly Mock<IImportDeduplicationService> _deduplicationService = new();
    private readonly Mock<ITransferMatchingService> _transferMatchingService = new();

    private ProcessImportCommandHandler CreateHandler() =>
        new(_bankImportRepository.Object, _deduplicationService.Object, _transferMatchingService.Object);

    [Fact]
    public async Task Handle_DeveGravarQuantidadeDeDuplicatas_EConcluirImportacao()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Pending,
            Transactions = [new BankImportTransaction { Id = Guid.NewGuid(), Hash = "abc123" }],
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        _deduplicationService
            .Setup(s => s.DetectAsync(bankImport, default))
            .ReturnsAsync(1);

        // Act
        await CreateHandler().Handle(new ProcessImportCommand(importId, userId), default);

        // Assert
        bankImport.Duplicates.Should().Be(1);
        bankImport.Status.Should().Be(BankImportStatus.Completed);
        _bankImportRepository.Verify(r => r.UpdateAsync(bankImport, default), Times.AtLeastOnce);
    }

    // O pareamento de transferências só roda depois das duplicatas: linha duplicada nunca é ponta de transferência
    [Fact]
    public async Task Handle_DeveParearTransferencias_DepoisDeDetectarDuplicatas()
    {
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Pending,
            Transactions = [new BankImportTransaction { Id = Guid.NewGuid(), Hash = "abc" }],
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        var order = new List<string>();
        _deduplicationService
            .Setup(s => s.DetectAsync(bankImport, default))
            .Callback(() => order.Add("duplicatas"))
            .ReturnsAsync(0);
        _transferMatchingService
            .Setup(s => s.MatchAsync(bankImport, default))
            .Callback(() => order.Add("transferencias"))
            .ReturnsAsync(0);

        await CreateHandler().Handle(new ProcessImportCommand(importId, userId), default);

        order.Should().Equal("duplicatas", "transferencias");
    }

    // As duplicatas permanecem no staging para o usuário revisar ("importar mesmo assim")
    [Fact]
    public async Task Handle_NaoDeveRemoverLinhasDuplicadasDoStaging()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var importId = Guid.NewGuid();

        var duplicada = new BankImportTransaction { Id = Guid.NewGuid(), Hash = "dup" };
        var nova = new BankImportTransaction { Id = Guid.NewGuid(), Hash = "nova" };

        var bankImport = new BankImport
        {
            Id = importId,
            UserId = userId,
            Status = BankImportStatus.Pending,
            Transactions = [duplicada, nova],
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync(bankImport);

        _deduplicationService
            .Setup(s => s.DetectAsync(bankImport, default))
            .Callback<BankImport, CancellationToken>((_, _) =>
            {
                duplicada.IsDuplicate = true;
                duplicada.IsSelected = false;
            })
            .ReturnsAsync(1);

        // Act
        await CreateHandler().Handle(new ProcessImportCommand(importId, userId), default);

        // Assert
        bankImport.Transactions.Should().HaveCount(2);
        bankImport.Transactions.Should().Contain(duplicada);
    }

    [Fact]
    public async Task Handle_DeveIgnorar_QuandoImportacaoNaoExiste()
    {
        // Arrange
        var importId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, userId, default))
            .ReturnsAsync((BankImport?)null);

        // Act
        await CreateHandler().Handle(new ProcessImportCommand(importId, userId), default);

        // Assert
        _bankImportRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<BankImport>(), default), Times.Never);
        _deduplicationService.Verify(s =>
            s.DetectAsync(It.IsAny<BankImport>(), default), Times.Never);
    }
}
