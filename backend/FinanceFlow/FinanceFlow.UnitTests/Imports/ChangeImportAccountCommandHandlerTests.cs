using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Services;
using FinanceFlow.Application.UseCases.Imports.Commands.ChangeImportAccount;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Imports;

public class ChangeImportAccountCommandHandlerTests
{
    private readonly Mock<IBankImportRepository> _bankImportRepository = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<IImportDeduplicationService> _deduplicationService = new();
    private readonly Mock<ITransferMatchingService> _transferMatchingService = new();
    private readonly Guid _userId = Guid.NewGuid();

    private ChangeImportAccountCommandHandler CreateHandler() =>
        new(_bankImportRepository.Object, _accountRepository.Object, _deduplicationService.Object,
            _transferMatchingService.Object);

    private BankImport SetupImport(
        BankImportStatus status = BankImportStatus.Completed,
        int imported = 0,
        string? keyHash = null)
    {
        var bankImport = new BankImport
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            AccountId = Guid.NewGuid(),
            Status = status,
            Imported = imported,
            BankId = "001",
            AccountKeyHash = keyHash,
            AccountLast4 = keyHash is null ? null : "1234",
        };

        _bankImportRepository
            .Setup(r => r.GetByIdAsync(bankImport.Id, _userId, default))
            .ReturnsAsync(bankImport);

        return bankImport;
    }

    private Account SetupAccount(bool archived = false, string? keyHash = null)
    {
        var account = new Account
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            Name = "Nubank",
            IsArchived = archived,
            AccountKeyHash = keyHash,
        };

        _accountRepository
            .Setup(r => r.GetByIdAsync(account.Id, _userId, default))
            .ReturnsAsync(account);

        return account;
    }

    [Fact]
    public async Task Handle_DeveTrocarContaERefazerDeteccao_QuandoImportacaoConcluida()
    {
        var bankImport = SetupImport();
        var account = SetupAccount();
        _deduplicationService
            .Setup(s => s.DetectAsync(bankImport, default))
            .ReturnsAsync(2);

        var result = await CreateHandler().Handle(
            new ChangeImportAccountCommand(bankImport.Id, _userId, account.Id), default);

        bankImport.AccountId.Should().Be(account.Id);
        bankImport.Duplicates.Should().Be(2);
        result.AccountId.Should().Be(account.Id);
        result.AccountName.Should().Be("Nubank");
        _deduplicationService.Verify(s => s.DetectAsync(bankImport, default), Times.Once);
        _bankImportRepository.Verify(r => r.UpdateAsync(bankImport, default), Times.Once);
    }

    // O par de uma transferência precisa estar em OUTRA conta: trocar a conta muda os pares
    [Fact]
    public async Task Handle_DeveRefazerOPareamentoDeTransferencias_QuandoImportacaoConcluida()
    {
        var bankImport = SetupImport();
        var account = SetupAccount();

        await CreateHandler().Handle(
            new ChangeImportAccountCommand(bankImport.Id, _userId, account.Id), default);

        _transferMatchingService.Verify(s => s.MatchAsync(bankImport, default), Times.Once);
    }

    [Fact]
    public async Task Handle_NaoDeveParearTransferencias_QuandoImportacaoPendente()
    {
        var bankImport = SetupImport(BankImportStatus.Pending);
        var account = SetupAccount();

        await CreateHandler().Handle(
            new ChangeImportAccountCommand(bankImport.Id, _userId, account.Id), default);

        _transferMatchingService.Verify(s => s.MatchAsync(It.IsAny<BankImport>(), default), Times.Never);
    }

    // Pendente: o Worker ainda vai processar usando a nova conta
    [Fact]
    public async Task Handle_NaoDeveDetectarDuplicatas_QuandoImportacaoPendente()
    {
        var bankImport = SetupImport(BankImportStatus.Pending);
        var account = SetupAccount();

        await CreateHandler().Handle(
            new ChangeImportAccountCommand(bankImport.Id, _userId, account.Id), default);

        bankImport.AccountId.Should().Be(account.Id);
        _deduplicationService.Verify(s => s.DetectAsync(It.IsAny<BankImport>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRecusar_QuandoImportacaoJaConfirmada()
    {
        var bankImport = SetupImport(imported: 5);
        var account = SetupAccount();

        var act = () => CreateHandler().Handle(
            new ChangeImportAccountCommand(bankImport.Id, _userId, account.Id), default);

        await act.Should().ThrowAsync<ValidationException>();
        _bankImportRepository.Verify(r => r.UpdateAsync(It.IsAny<BankImport>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_DeveRecusar_QuandoImportacaoEmProcessamento()
    {
        var bankImport = SetupImport(BankImportStatus.Processing);
        var account = SetupAccount();

        var act = () => CreateHandler().Handle(
            new ChangeImportAccountCommand(bankImport.Id, _userId, account.Id), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_DeveRecusar_QuandoContaArquivada()
    {
        var bankImport = SetupImport();
        var account = SetupAccount(archived: true);

        var act = () => CreateHandler().Handle(
            new ChangeImportAccountCommand(bankImport.Id, _userId, account.Id), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Handle_DeveLancarNotFound_QuandoContaNaoExiste()
    {
        var bankImport = SetupImport();
        var missing = Guid.NewGuid();
        _accountRepository
            .Setup(r => r.GetByIdAsync(missing, _userId, default))
            .ReturnsAsync((Account?)null);

        var act = () => CreateHandler().Handle(
            new ChangeImportAccountCommand(bankImport.Id, _userId, missing), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_DeveLancarNotFound_QuandoImportacaoNaoExiste()
    {
        var importId = Guid.NewGuid();
        _bankImportRepository
            .Setup(r => r.GetByIdAsync(importId, _userId, default))
            .ReturnsAsync((BankImport?)null);

        var act = () => CreateHandler().Handle(
            new ChangeImportAccountCommand(importId, _userId, Guid.NewGuid()), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // O arquivo já pertence a outra conta do usuário
    [Fact]
    public async Task Handle_DeveRecusar_QuandoArquivoPertenceAOutraConta()
    {
        var bankImport = SetupImport(keyHash: "chave");
        var chosen = SetupAccount();
        var owner = new Account { Id = Guid.NewGuid(), UserId = _userId, Name = "Itaú" };
        _accountRepository
            .Setup(r => r.GetByKeyAsync(_userId, "001", "chave", default))
            .ReturnsAsync(owner);

        var act = () => CreateHandler().Handle(
            new ChangeImportAccountCommand(bankImport.Id, _userId, chosen.Id), default);

        var ex = await act.Should().ThrowAsync<ValidationException>();
        ex.Which.Message.Should().Contain("Itaú");
        bankImport.AccountId.Should().NotBe(chosen.Id);
    }

    // Conta sem vínculo passa a reconhecer o arquivo nas próximas importações
    [Fact]
    public async Task Handle_DeveAprenderChaveDaConta_QuandoContaAindaNaoTemVinculo()
    {
        var bankImport = SetupImport(keyHash: "chave");
        var chosen = SetupAccount();
        _accountRepository
            .Setup(r => r.GetByKeyAsync(_userId, "001", "chave", default))
            .ReturnsAsync((Account?)null);

        await CreateHandler().Handle(
            new ChangeImportAccountCommand(bankImport.Id, _userId, chosen.Id), default);

        chosen.AccountKeyHash.Should().Be("chave");
        chosen.BankId.Should().Be("001");
        chosen.AccountLast4.Should().Be("1234");
        _accountRepository.Verify(r => r.UpdateAsync(chosen, default), Times.Once);
    }
}
