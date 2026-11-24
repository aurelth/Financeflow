using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Services;
using FinanceFlow.Application.UseCases.Accounts.Queries.GetAccountReconciliation;
using FinanceFlow.Application.UseCases.Accounts.Queries.GetAccountsReconciliation;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Accounts;

public class AccountReconciliationTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly DateTime StatementDate = new(2026, 2, 28);

    private readonly Mock<IBankImportRepository> _bankImportRepository = new();
    private readonly Mock<IAccountRepository> _accountRepository = new();

    private AccountReconciliationService CreateService() =>
        new(_bankImportRepository.Object, _accountRepository.Object);

    private static Account NewAccount(decimal initialBalance = 0m, DateTime? initialDate = null) => new()
    {
        Id = Guid.NewGuid(),
        UserId = UserId,
        Name = "Nubank",
        InitialBalance = initialBalance,
        InitialBalanceDate = initialDate,
    };

    private void GivenStatement(Account account, decimal? balance, DateTime? date = null)
    {
        _bankImportRepository
            .Setup(r => r.GetLatestStatementAsync(UserId, account.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankImport
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                AccountId = account.Id,
                FileName = "fevereiro.ofx",
                LedgerBalance = balance,
                LedgerBalanceDate = balance is null ? null : date ?? StatementDate,
                StatementStart = new DateTime(2026, 2, 1),
                StatementEnd = new DateTime(2026, 2, 28),
                ConfirmedAt = DateTime.UtcNow,
            });
    }

    private void GivenMovements(Account account, decimal total) =>
        _accountRepository
            .Setup(r => r.GetMovementTotalUntilAsync(UserId, account.Id, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(total);

    [Fact]
    public async Task Reconciliar_DeveInformarSemExtrato_QuandoNenhumaImportacaoConfirmadaTrouxeSaldo()
    {
        var account = NewAccount();

        var result = await CreateService().ReconcileAsync(account);

        result.Status.Should().Be(ReconciliationStatus.NoStatement);
        result.Difference.Should().BeNull();
        result.StatementBalance.Should().BeNull();
        _accountRepository.Verify(r => r.GetMovementTotalUntilAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Reconciliar_DeveInformarSemExtrato_QuandoImportacaoNaoTemSaldo()
    {
        var account = NewAccount();
        GivenStatement(account, balance: null);

        var result = await CreateService().ReconcileAsync(account);

        result.Status.Should().Be(ReconciliationStatus.NoStatement);
    }

    [Fact]
    public async Task Reconciliar_DeveConciliar_QuandoSaldoCalculadoIgualAoDoExtrato()
    {
        var account = NewAccount(initialBalance: 1000m);
        GivenStatement(account, balance: 1450m);
        GivenMovements(account, 450m);

        var result = await CreateService().ReconcileAsync(account);

        result.Status.Should().Be(ReconciliationStatus.Matched);
        result.StatementBalance.Should().Be(1450m);
        result.CalculatedBalance.Should().Be(1450m);
        result.Difference.Should().Be(0m);
        result.StatementDate.Should().Be(StatementDate);
        result.FileName.Should().Be("fevereiro.ofx");
    }

    [Fact]
    public async Task Reconciliar_DeveApontarDiferencaPositiva_QuandoFaltamEntradasNoFinanceFlow()
    {
        var account = NewAccount(initialBalance: 1000m);
        GivenStatement(account, balance: 1500m);
        GivenMovements(account, 400m);

        var result = await CreateService().ReconcileAsync(account);

        result.Status.Should().Be(ReconciliationStatus.Divergent);
        result.CalculatedBalance.Should().Be(1400m);
        result.Difference.Should().Be(100m);
    }

    [Fact]
    public async Task Reconciliar_DeveApontarDiferencaNegativa_QuandoSobramEntradasNoFinanceFlow()
    {
        var account = NewAccount();
        GivenStatement(account, balance: 200m);
        GivenMovements(account, 250.50m);

        var result = await CreateService().ReconcileAsync(account);

        result.Status.Should().Be(ReconciliationStatus.Divergent);
        result.Difference.Should().Be(-50.50m);
    }

    [Fact]
    public async Task Reconciliar_DeveAceitarSaldoNegativoDoExtrato()
    {
        var account = NewAccount();
        GivenStatement(account, balance: -120.10m);
        GivenMovements(account, -120.10m);

        var result = await CreateService().ReconcileAsync(account);

        result.Status.Should().Be(ReconciliationStatus.Matched);
    }

    [Fact]
    public async Task Reconciliar_DeveArredondarADiferencaEmCentavos()
    {
        var account = NewAccount();
        GivenStatement(account, balance: 100.00m);
        GivenMovements(account, 99.999m);

        var result = await CreateService().ReconcileAsync(account);

        // 100,00 - 99,999 = 0,001 -> arredonda para 0,00: sem diferença real
        result.Status.Should().Be(ReconciliationStatus.Matched);
        result.Difference.Should().Be(0m);
    }

    [Fact]
    public async Task Reconciliar_DeveFicarIndisponivel_QuandoSaldoInicialEPosteriorAoExtrato()
    {
        var account = NewAccount(initialBalance: 500m, initialDate: new DateTime(2026, 3, 15));
        GivenStatement(account, balance: 800m);

        var result = await CreateService().ReconcileAsync(account);

        result.Status.Should().Be(ReconciliationStatus.Unavailable);
        result.Difference.Should().BeNull();
        result.StatementBalance.Should().Be(800m);
        result.Message.Should().Contain("saldo inicial");
    }

    [Fact]
    public async Task Reconciliar_DevePermitirSaldoInicialNoMesmoDiaDoExtrato()
    {
        var account = NewAccount(initialBalance: 500m, initialDate: StatementDate);
        GivenStatement(account, balance: 500m);
        GivenMovements(account, 0m);

        var result = await CreateService().ReconcileAsync(account);

        result.Status.Should().Be(ReconciliationStatus.Matched);
    }

    [Fact]
    public async Task Reconciliar_DeveCalcularAteADataDoExtrato()
    {
        var account = NewAccount();
        GivenStatement(account, balance: 10m);
        GivenMovements(account, 10m);

        await CreateService().ReconcileAsync(account);

        _accountRepository.Verify(r => r.GetMovementTotalUntilAsync(
            UserId, account.Id, StatementDate, It.IsAny<CancellationToken>()), Times.Once);
    }

    // Handlers

    [Fact]
    public async Task ConciliacaoDaConta_DeveLancarNotFound_QuandoContaNaoPertenceAoUsuario()
    {
        _accountRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);
        var handler = new GetAccountReconciliationQueryHandler(
            _accountRepository.Object, Mock.Of<IAccountReconciliationService>());

        var act = () => handler.Handle(new GetAccountReconciliationQuery(Guid.NewGuid(), UserId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task ConciliacaoDeTodasAsContas_DeveConsiderarSomenteContasAtivas()
    {
        var a = NewAccount();
        var b = NewAccount();
        _accountRepository
            .Setup(r => r.GetByUserAsync(UserId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync([a, b]);
        GivenStatement(a, balance: 10m);
        GivenMovements(a, 10m);
        var handler = new GetAccountsReconciliationQueryHandler(
            _accountRepository.Object, CreateService());

        var result = (await handler.Handle(new GetAccountsReconciliationQuery(UserId), default)).ToList();

        result.Should().HaveCount(2);
        result.Single(r => r.AccountId == a.Id).Status.Should().Be(ReconciliationStatus.Matched);
        result.Single(r => r.AccountId == b.Id).Status.Should().Be(ReconciliationStatus.NoStatement);
    }
}
