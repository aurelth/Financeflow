using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Dashboard.Queries.GetDashboardSummary;

public class GetDashboardSummaryQueryHandler(
    ITransactionRepository transactionRepository)
    : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    public async Task<DashboardSummaryDto> Handle(
        GetDashboardSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var dateFrom = new DateTime(request.Year, request.Month, 1);
        var dateTo = dateFrom.AddMonths(1).AddDays(-1);

        var (transactions, _) = await transactionRepository.GetPagedByUserAsync(
            userId: request.UserId,
            page: 1,
            pageSize: int.MaxValue,
            dateFrom: dateFrom,
            dateTo: dateTo,
            categoryId: null,
            subcategoryId: null,
            type: null,
            status: null,
            amountMin: null,
            amountMax: null,
            search: null,
            cancellationToken: cancellationToken);

        var totalIncome = transactions
            .Where(t => t.Type == TransactionType.Income && t.Status != TransactionStatus.Scheduled)
            .Sum(t => t.Amount);

        var totalExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense && t.Status != TransactionStatus.Scheduled)
            .Sum(t => t.Amount);

        var balance = totalIncome - totalExpenses;

        // Saldo projetado = saldo atual + transações agendadas
        var projectedIncome = transactions
            .Where(t => t.Type == TransactionType.Income && t.Status == TransactionStatus.Scheduled)
            .Sum(t => t.Amount);

        var projectedExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense && t.Status == TransactionStatus.Scheduled)
            .Sum(t => t.Amount);

        var projectedBalance = balance + projectedIncome - projectedExpenses;

        return new DashboardSummaryDto(
            TotalIncome: totalIncome,
            TotalExpenses: totalExpenses,
            Balance: balance,
            ProjectedBalance: projectedBalance,
            Month: request.Month,
            Year: request.Year);
    }
}
