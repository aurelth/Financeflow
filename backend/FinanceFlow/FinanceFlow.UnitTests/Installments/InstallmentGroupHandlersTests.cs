using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.Services;
using FinanceFlow.Application.UseCases.Transactions.Commands.CancelInstallments;
using FinanceFlow.Application.UseCases.Transactions.Commands.DeleteTransaction;
using FinanceFlow.Application.UseCases.Transactions.Commands.UpdateTransaction;
using FinanceFlow.Application.UseCases.Transactions.Queries.GetInstallmentPlan;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Installments;

public class InstallmentGroupHandlersTests
{
    private readonly Mock<ITransactionRepository> _transactions = new();
    private readonly Mock<ICategoryRepository> _categories = new();
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<ITransferRepository> _transfers = new();
    private readonly Mock<ICacheService> _cache = new();

    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid GroupId = Guid.NewGuid();
    private static readonly Guid CategoryId = Guid.NewGuid();
    private static readonly Guid OtherCategoryId = Guid.NewGuid();

    private static Category Cat(Guid id, string name) =>
        new() { Id = id, Name = name, Icon = "x", Color = "#000", Type = TransactionType.Expense, UserId = UserId };

    public InstallmentGroupHandlersTests()
    {
        _cache.Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transactions.Setup(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transactions.Setup(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Transaction>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _categories.Setup(c => c.GetByIdAsync(CategoryId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Cat(CategoryId, "Eletrônicos"));
        _categories.Setup(c => c.GetByIdAsync(OtherCategoryId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(Cat(OtherCategoryId, "Casa"));
    }

    private IDashboardCacheInvalidator Invalidator => new DashboardCacheInvalidator(_cache.Object);

    private static Transaction Parcel(int number, TransactionStatus status = TransactionStatus.Scheduled, decimal amount = 100m, int count = 6) => new()
    {
        Id = Guid.NewGuid(),
        UserId = UserId,
        Amount = amount,
        Type = TransactionType.Expense,
        Date = new DateTime(2026, 11, 10).AddMonths(number - 1),
        Description = "Notebook",
        Status = status,
        CategoryId = CategoryId,
        Category = Cat(CategoryId, "Eletrônicos"),
        Tags = "[]",
        InstallmentGroupId = GroupId,
        InstallmentNumber = number,
        InstallmentCount = count,
    };

    // Update

    private UpdateTransactionCommandHandler UpdateHandler() =>
        new(_transactions.Object, _categories.Object, _accounts.Object, _transfers.Object, Invalidator);

    private static UpdateTransactionCommand UpdateOf(Transaction t, Guid? categoryId = null, decimal? amount = null,
        TransactionType? type = null, bool recurring = false, bool propagate = false) =>
        new(t.Id, UserId, amount ?? t.Amount, type ?? t.Type, t.Date, t.Description, t.Status, recurring,
            RecurrenceType.None, categoryId ?? t.CategoryId, null, [], PropagateToFuture: propagate);

    // O handler zera a navegação da entidade carregada: a recarga final devolve outra instância
    private void ArrangeLoad(Transaction current)
    {
        var reloaded = Parcel(current.InstallmentNumber!.Value);
        reloaded.Id = current.Id;
        _transactions.SetupSequence(r => r.GetByIdAsync(current.Id, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(current)
            .ReturnsAsync(reloaded);
    }

    [Fact]
    public async Task Update_RecusaMudarOTipoDeUmaParcela()
    {
        var parcel = Parcel(2);
        ArrangeLoad(parcel);
        _categories.Setup(c => c.GetByIdAsync(OtherCategoryId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = OtherCategoryId, Name = "Salário", Icon = "x", Color = "#000", Type = TransactionType.Income, UserId = UserId });

        var act = () => UpdateHandler().Handle(UpdateOf(parcel, categoryId: OtherCategoryId, type: TransactionType.Income), default);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Message.Should().Contain("tipo");
        _transactions.Verify(r => r.UpdateAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_RecusaTornarUmaParcelaRecorrente()
    {
        var parcel = Parcel(2);
        ArrangeLoad(parcel);

        var act = () => UpdateHandler().Handle(UpdateOf(parcel, recurring: true), default);

        (await act.Should().ThrowAsync<ValidationException>()).Which.Message.Should().Contain("recorrente");
    }

    [Fact]
    public async Task Update_PropagaParaAsParcelasSeguintesNaoPagas_PorNumero()
    {
        var parcel = Parcel(3);
        var next = new List<Transaction> { Parcel(4), Parcel(5) };
        ArrangeLoad(parcel);
        _transactions
            .Setup(r => r.GetFutureInstallmentsAsync(GroupId, UserId, 3, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(next);

        await UpdateHandler().Handle(UpdateOf(parcel, categoryId: OtherCategoryId, amount: 150m, propagate: true), default);

        _transactions.Verify(r => r.UpdateRangeAsync(
            It.Is<IEnumerable<Transaction>>(ts => ts.Count() == 2 && ts.All(t => t.Amount == 150m && t.CategoryId == OtherCategoryId)),
            It.IsAny<CancellationToken>()), Times.Once);
        _transactions.Verify(r => r.GetFutureRecurringAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_NaoPropaga_QuandoNaoPediu()
    {
        var parcel = Parcel(3);
        ArrangeLoad(parcel);

        await UpdateHandler().Handle(UpdateOf(parcel, amount: 150m, propagate: false), default);

        _transactions.Verify(r => r.GetFutureInstallmentsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _transactions.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Transaction>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_EditarSoAData_NaoDisparaPropagacao()
    {
        var parcel = Parcel(3);
        ArrangeLoad(parcel);

        var command = UpdateOf(parcel, propagate: true) with { Date = parcel.Date.AddDays(3) };
        await UpdateHandler().Handle(command, default);

        _transactions.Verify(r => r.GetFutureInstallmentsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Delete

    private DeleteTransactionCommandHandler DeleteHandler() => new(_transactions.Object, _transfers.Object, Invalidator);

    [Fact]
    public async Task Delete_ComDeleteFuture_RemoveASeguintesNaoPagasDoGrupo()
    {
        var parcel = Parcel(2);
        var next = new List<Transaction> { Parcel(3), Parcel(4, TransactionStatus.Pending) };
        _transactions.Setup(r => r.GetByIdAsync(parcel.Id, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(parcel);
        _transactions.Setup(r => r.GetFutureInstallmentsAsync(GroupId, UserId, 2, true, It.IsAny<CancellationToken>())).ReturnsAsync(next);

        await DeleteHandler().Handle(new DeleteTransactionCommand(parcel.Id, UserId, DeleteFuture: true), default);

        parcel.DeletedAt.Should().NotBeNull();
        next.Should().OnlyContain(p => p.DeletedAt != null);
        _transactions.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<Transaction>>(ts => ts.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_SemDeleteFuture_RemoveSoAParcelaEscolhida()
    {
        var parcel = Parcel(2);
        _transactions.Setup(r => r.GetByIdAsync(parcel.Id, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(parcel);

        await DeleteHandler().Handle(new DeleteTransactionCommand(parcel.Id, UserId), default);

        parcel.DeletedAt.Should().NotBeNull();
        _transactions.Verify(r => r.GetFutureInstallmentsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _transactions.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Transaction>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Delete_InvalidaOCacheDeCadaMesRemovido()
    {
        var parcel = Parcel(1);
        var next = new List<Transaction> { Parcel(2), Parcel(3) };
        _transactions.Setup(r => r.GetByIdAsync(parcel.Id, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(parcel);
        _transactions.Setup(r => r.GetFutureInstallmentsAsync(GroupId, UserId, 1, true, It.IsAny<CancellationToken>())).ReturnsAsync(next);

        await DeleteHandler().Handle(new DeleteTransactionCommand(parcel.Id, UserId, DeleteFuture: true), default);

        foreach (var (year, month) in new[] { (2026, 11), (2026, 12), (2027, 1) })
            _cache.Verify(c => c.RemoveAsync($"dashboard:summary:{UserId}:{year}:{month}", It.IsAny<CancellationToken>()), Times.Once);
    }

    // Cancelar a compra

    private CancelInstallmentsCommandHandler CancelHandler() => new(_transactions.Object, Invalidator);

    [Fact]
    public async Task Cancel_RemoveAsNaoPagas_EMantemAsPagas()
    {
        var group = new List<Transaction>
        {
            Parcel(1, TransactionStatus.Paid), Parcel(2, TransactionStatus.Paid),
            Parcel(3, TransactionStatus.Pending), Parcel(4), Parcel(5), Parcel(6),
        };
        _transactions.Setup(r => r.GetInstallmentGroupAsync(GroupId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(group);

        var result = await CancelHandler().Handle(new CancelInstallmentsCommand(GroupId, UserId), default);

        result.Should().Be(new FinanceFlow.Application.DTOs.CancelInstallmentsResultDto(4, 2));
        group.Where(p => p.Status == TransactionStatus.Paid).Should().OnlyContain(p => p.DeletedAt == null);
        group.Where(p => p.Status != TransactionStatus.Paid).Should().OnlyContain(p => p.DeletedAt != null);
        _transactions.Verify(r => r.UpdateRangeAsync(It.Is<IEnumerable<Transaction>>(ts => ts.Count() == 4), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_SemParcelasAbertas_NaoGravaNada()
    {
        var group = new List<Transaction> { Parcel(1, TransactionStatus.Paid), Parcel(2, TransactionStatus.Paid) };
        _transactions.Setup(r => r.GetInstallmentGroupAsync(GroupId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(group);

        var result = await CancelHandler().Handle(new CancelInstallmentsCommand(GroupId, UserId), default);

        result.CancelledCount.Should().Be(0);
        result.KeptPaidCount.Should().Be(2);
        _transactions.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<Transaction>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Cancel_GrupoInexistenteOuDeOutroUsuario_EhNaoEncontrado()
    {
        _transactions.Setup(r => r.GetInstallmentGroupAsync(GroupId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var act = () => CancelHandler().Handle(new CancelInstallmentsCommand(GroupId, UserId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cancel_InvalidaOCacheDosMesesCancelados()
    {
        var group = new List<Transaction> { Parcel(1, TransactionStatus.Paid), Parcel(2), Parcel(3) };
        _transactions.Setup(r => r.GetInstallmentGroupAsync(GroupId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(group);

        await CancelHandler().Handle(new CancelInstallmentsCommand(GroupId, UserId), default);

        _cache.Verify(c => c.RemoveAsync($"dashboard:summary:{UserId}:2026:12", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync($"dashboard:summary:{UserId}:2027:1", It.IsAny<CancellationToken>()), Times.Once);
        // O mês da parcela paga não foi tocado
        _cache.Verify(c => c.RemoveAsync($"dashboard:summary:{UserId}:2026:11", It.IsAny<CancellationToken>()), Times.Never);
    }

    // Plano

    private GetInstallmentPlanQueryHandler PlanHandler() => new(_transactions.Object);

    [Fact]
    public async Task Plano_SomaTotalPagoERestante()
    {
        var group = new List<Transaction>
        {
            Parcel(1, TransactionStatus.Paid, 100m, 3), Parcel(2, TransactionStatus.Pending, 100m, 3), Parcel(3, TransactionStatus.Scheduled, 100.50m, 3),
        };
        _transactions.Setup(r => r.GetInstallmentGroupAsync(GroupId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(group);

        var plan = await PlanHandler().Handle(new GetInstallmentPlanQuery(GroupId, UserId), default);

        plan.Count.Should().Be(3);
        plan.TotalAmount.Should().Be(300.50m);
        plan.PaidAmount.Should().Be(100m);
        plan.RemainingAmount.Should().Be(200.50m);
        plan.Description.Should().Be("Notebook");
        plan.Installments.Select(i => i.Number).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Plano_MantemOTotalOriginalDeParcelas_MesmoComParcelasCanceladas()
    {
        // Sobraram a 1 e a 2 de um plano de 6
        var group = new List<Transaction> { Parcel(1, TransactionStatus.Paid), Parcel(2, TransactionStatus.Paid) };
        _transactions.Setup(r => r.GetInstallmentGroupAsync(GroupId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync(group);

        var plan = await PlanHandler().Handle(new GetInstallmentPlanQuery(GroupId, UserId), default);

        plan.Count.Should().Be(6);
        plan.Installments.Should().HaveCount(2);
    }

    [Fact]
    public async Task Plano_GrupoInexistente_EhNaoEncontrado()
    {
        _transactions.Setup(r => r.GetInstallmentGroupAsync(GroupId, UserId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var act = () => PlanHandler().Handle(new GetInstallmentPlanQuery(GroupId, UserId), default);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
