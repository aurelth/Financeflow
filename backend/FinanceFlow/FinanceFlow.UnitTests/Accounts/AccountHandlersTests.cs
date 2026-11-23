using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.UseCases.Accounts.Commands.ArchiveAccount;
using FinanceFlow.Application.UseCases.Accounts.Commands.CreateAccount;
using FinanceFlow.Application.UseCases.Accounts.Commands.UpdateAccount;
using FinanceFlow.Application.UseCases.Accounts.Queries.GetAccounts;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Accounts;

public class AccountHandlersTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private readonly Mock<IAccountRepository> _repository = new();

    private static Account NewAccount(string name = "Nubank", bool isDefault = false, bool archived = false) => new()
    {
        Id = Guid.NewGuid(),
        UserId = UserId,
        Name = name,
        Type = AccountType.Checking,
        IsDefault = isDefault,
        IsArchived = archived,
    };

    // ── CreateAccount ─────────────────────────────────────────────────────

    [Fact]
    public async Task CriarConta_DeveSalvarComNomeSemEspacosEValoresPadrao()
    {
        // Arrange
        var handler = new CreateAccountCommandHandler(_repository.Object);
        var command = new CreateAccountCommand(
            UserId, "  Itaú  ", AccountType.Savings, " Itaú Unibanco ", 1500m, null, null, null, null);

        // Act
        var result = await handler.Handle(command, default);

        // Assert
        result.Name.Should().Be("Itaú");
        result.BankName.Should().Be("Itaú Unibanco");
        result.Type.Should().Be(AccountType.Savings);
        result.Currency.Should().Be("BRL");
        result.Color.Should().Be("#10B981");
        result.IsDefault.Should().BeFalse();
        result.Balance.Should().Be(1500m);

        _repository.Verify(r => r.AddAsync(
            It.Is<Account>(a => a.UserId == UserId && a.Name == "Itaú" && !a.IsDefault),
            default), Times.Once);
    }

    [Fact]
    public async Task CriarConta_DeveLancarValidacao_QuandoNomeJaExiste()
    {
        // Arrange
        _repository
            .Setup(r => r.NameExistsAsync(UserId, "Nubank", null, default))
            .ReturnsAsync(true);

        var handler = new CreateAccountCommandHandler(_repository.Object);
        var command = new CreateAccountCommand(
            UserId, "Nubank", AccountType.Checking, null, 0m, null, null, null, null);

        // Act
        var act = async () => await handler.Handle(command, default);

        // Assert
        await act.Should().ThrowAsync<ValidationException>();
        _repository.Verify(r => r.AddAsync(It.IsAny<Account>(), default), Times.Never);
    }

    [Fact]
    public void ValidadorCriarConta_DeveRejeitarNomeVazioECorInvalida()
    {
        var validator = new CreateAccountCommandValidator();
        var command = new CreateAccountCommand(
            UserId, "", AccountType.Checking, null, 0m, null, null, "verde", null);

        var result = validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Select(e => e.PropertyName).Should().Contain(["Name", "Color"]);
    }

    // ── UpdateAccount ─────────────────────────────────────────────────────

    [Fact]
    public async Task AtualizarConta_DeveLancarNotFound_QuandoContaNaoExiste()
    {
        var handler = new UpdateAccountCommandHandler(_repository.Object);
        var command = new UpdateAccountCommand(
            Guid.NewGuid(), UserId, "X", AccountType.Checking, null, 0m, null, null, null, null, false);

        var act = async () => await handler.Handle(command, default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AtualizarConta_NaoDevePermitirArquivarContaPrincipal()
    {
        var account = NewAccount("Conta principal", isDefault: true);
        _repository.Setup(r => r.GetByIdAsync(account.Id, UserId, default)).ReturnsAsync(account);

        var handler = new UpdateAccountCommandHandler(_repository.Object);
        var command = new UpdateAccountCommand(
            account.Id, UserId, "Conta principal", AccountType.Checking, null, 0m, null, null, null, null, true);

        var act = async () => await handler.Handle(command, default);

        await act.Should().ThrowAsync<ValidationException>();
        _repository.Verify(r => r.UpdateAsync(It.IsAny<Account>(), default), Times.Never);
    }

    [Fact]
    public async Task AtualizarConta_DeveAplicarAlteracoesEReativar()
    {
        var account = NewAccount("Antiga", archived: true);
        _repository.Setup(r => r.GetByIdAsync(account.Id, UserId, default)).ReturnsAsync(account);
        _repository
            .Setup(r => r.GetMovementTotalsAsync(UserId, default))
            .ReturnsAsync(new Dictionary<Guid, decimal> { [account.Id] = 250m });

        var handler = new UpdateAccountCommandHandler(_repository.Object);
        var command = new UpdateAccountCommand(
            account.Id, UserId, "Nova", AccountType.CreditCard, "Banco X", 100m, null, "usd", "#112233", "credit-card", false);

        var result = await handler.Handle(command, default);

        result.Name.Should().Be("Nova");
        result.Type.Should().Be(AccountType.CreditCard);
        result.Currency.Should().Be("USD");
        result.Color.Should().Be("#112233");
        result.IsArchived.Should().BeFalse();
        result.Balance.Should().Be(350m); // saldo inicial 100 + movimentação 250
    }

    // ── ArchiveAccount ────────────────────────────────────────────────────

    [Fact]
    public async Task ArquivarConta_DeveMarcarComoArquivada()
    {
        var account = NewAccount();
        _repository.Setup(r => r.GetByIdAsync(account.Id, UserId, default)).ReturnsAsync(account);

        await new ArchiveAccountCommandHandler(_repository.Object)
            .Handle(new ArchiveAccountCommand(account.Id, UserId), default);

        account.IsArchived.Should().BeTrue();
        _repository.Verify(r => r.UpdateAsync(account, default), Times.Once);
    }

    [Fact]
    public async Task ArquivarConta_NaoDevePermitirContaPrincipal()
    {
        var account = NewAccount("Conta principal", isDefault: true);
        _repository.Setup(r => r.GetByIdAsync(account.Id, UserId, default)).ReturnsAsync(account);

        var act = async () => await new ArchiveAccountCommandHandler(_repository.Object)
            .Handle(new ArchiveAccountCommand(account.Id, UserId), default);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task ArquivarConta_DeveLancarNotFound_QuandoContaNaoExiste()
    {
        var act = async () => await new ArchiveAccountCommandHandler(_repository.Object)
            .Handle(new ArchiveAccountCommand(Guid.NewGuid(), UserId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetAccounts ───────────────────────────────────────────────────────

    [Fact]
    public async Task ListarContas_DeveSomarSaldoInicialComMovimentacaoEGarantirContaPrincipal()
    {
        var principal = NewAccount("Conta principal", isDefault: true);
        var nubank = NewAccount("Nubank");
        nubank.InitialBalance = 1000m;

        _repository.Setup(r => r.GetOrCreateDefaultAsync(UserId, default)).ReturnsAsync(principal);
        _repository.Setup(r => r.GetByUserAsync(UserId, false, default))
            .ReturnsAsync(new[] { principal, nubank });
        _repository.Setup(r => r.GetMovementTotalsAsync(UserId, default))
            .ReturnsAsync(new Dictionary<Guid, decimal> { [nubank.Id] = -300m });

        var result = (await new GetAccountsQueryHandler(_repository.Object)
            .Handle(new GetAccountsQuery(UserId), default)).ToList();

        result.Should().HaveCount(2);
        result.Single(a => a.Id == principal.Id).Balance.Should().Be(0m);
        result.Single(a => a.Id == nubank.Id).Balance.Should().Be(700m);
        _repository.Verify(r => r.GetOrCreateDefaultAsync(UserId, default), Times.Once);
    }
}
