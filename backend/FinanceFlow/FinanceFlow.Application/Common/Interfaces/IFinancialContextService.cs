namespace FinanceFlow.Application.Common.Interfaces;

public interface IFinancialContextService
{
    Task<string> BuildContextAsync(
    Guid userId,
    int month,
    int year,
    CancellationToken cancellationToken = default);
}
