using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetAnnualSummary;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetCashFlow;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetProjections;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetReportByCategory;
using FinanceFlow.Application.UseCases.Analytics.Queries.GetReportByTag;
using FinanceFlow.Application.UseCases.Dashboard.Queries.GetPeriodComparison;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace FinanceFlow.UnitTests.Analytics;

/// <summary>
/// Relatórios filtrados por conta: o accountId chega ao repositório e a consulta
/// não passa pelo cache (a invalidação do cache é por chave exata, sem conta).
/// </summary>
public class AnalyticsAccountFilterTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly Guid AccountId = Guid.NewGuid();

    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<ICacheService> _cache = new();

    public AnalyticsAccountFilterTests()
    {
        _transactionRepository
            .Setup(r => r.GetPagedByUserAsync(
                It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
                It.IsAny<Guid?>(), It.IsAny<Guid?>(),
                It.IsAny<TransactionType?>(), It.IsAny<TransactionStatus?>(),
                It.IsAny<decimal?>(), It.IsAny<decimal?>(),
                It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Enumerable.Empty<Transaction>(), 0));
    }

    private void VerifyFilteredByAccount() =>
        _transactionRepository.Verify(r => r.GetPagedByUserAsync(
            UserId, It.IsAny<int>(), It.IsAny<int>(),
            It.IsAny<DateTime?>(), It.IsAny<DateTime?>(),
            It.IsAny<Guid?>(), It.IsAny<Guid?>(),
            It.IsAny<TransactionType?>(), It.IsAny<TransactionStatus?>(),
            It.IsAny<decimal?>(), It.IsAny<decimal?>(),
            It.IsAny<string?>(), AccountId, It.IsAny<CancellationToken>()), Times.AtLeastOnce);

    [Fact]
    public async Task FluxoDeCaixa_DeveFiltrarPorConta_SemUsarCache()
    {
        var handler = new GetCashFlowQueryHandler(_transactionRepository.Object, _cache.Object);

        var result = await handler.Handle(new GetCashFlowQuery(
            UserId, new DateTime(2026, 1, 1), new DateTime(2026, 2, 28), "month", AccountId), default);

        result.Should().NotBeNull();
        VerifyFilteredByAccount();
        _cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResumoAnual_DeveFiltrarPorConta_SemUsarCache()
    {
        var handler = new GetAnnualSummaryQueryHandler(_transactionRepository.Object, _cache.Object);

        var result = await handler.Handle(new GetAnnualSummaryQuery(UserId, 2026, AccountId), default);

        result.Should().NotBeNull();
        VerifyFilteredByAccount();
        _cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PorCategoria_DeveFiltrarPorConta_SemUsarCache()
    {
        var handler = new GetReportByCategoryQueryHandler(_transactionRepository.Object, _cache.Object);

        var result = await handler.Handle(new GetReportByCategoryQuery(
            UserId, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), null, AccountId), default);

        result.Should().NotBeNull();
        VerifyFilteredByAccount();
        _cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PorTag_DeveFiltrarPorConta_SemUsarCache()
    {
        var handler = new GetReportByTagQueryHandler(_transactionRepository.Object, _cache.Object);

        var result = await handler.Handle(new GetReportByTagQuery(
            UserId, new DateTime(2026, 1, 1), new DateTime(2026, 1, 31), AccountId), default);

        result.Should().NotBeNull();
        VerifyFilteredByAccount();
        _cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Projecoes_DeveFiltrarPorConta_SemUsarCache()
    {
        var handler = new GetProjectionsQueryHandler(_transactionRepository.Object, _cache.Object);

        var result = await handler.Handle(new GetProjectionsQuery(UserId, 6, 3, AccountId), default);

        result.Should().NotBeNull();
        VerifyFilteredByAccount();
        _cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ComparativoDePeriodos_DeveFiltrarPorConta_SemUsarCache()
    {
        var handler = new GetPeriodComparisonQueryHandler(_transactionRepository.Object, _cache.Object);

        var result = await handler.Handle(new GetPeriodComparisonQuery(
            UserId, new[] { (1, 2026), (2, 2026) }, AccountId), default);

        result.Should().NotBeNull();
        VerifyFilteredByAccount();
        _cache.VerifyNoOtherCalls();
    }
}
