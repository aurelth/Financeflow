using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Analytics.Queries.GetReportByCategory;

public class GetReportByCategoryQueryHandler(
    ITransactionRepository transactionRepository,
    ICacheService cache)
    : IRequestHandler<GetReportByCategoryQuery, ReportByCategoryDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public async Task<ReportByCategoryDto> Handle(
        GetReportByCategoryQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"analytics:by-category:{request.UserId}:{request.From:yyyyMMdd}:{request.To:yyyyMMdd}:{request.Type}";

        return await cache.GetOrSetAsync(cacheKey, async () =>
        {
            var (transactions, _) = await transactionRepository.GetPagedByUserAsync(
                userId: request.UserId,
                page: 1,
                pageSize: int.MaxValue,
                dateFrom: request.From,
                dateTo: request.To,
                categoryId: null,
                subcategoryId: null,
                type: request.Type,
                status: null,
                amountMin: null,
                amountMax: null,
                search: null,
                cancellationToken: cancellationToken);

            var confirmed = transactions
                .Where(t => t.Status != TransactionStatus.Scheduled)
                .ToList();

            var totalExpenses = confirmed.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);
            var totalIncome = confirmed.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);

            // Agrupa por categoria usando navigation property
            var categories = confirmed
                .GroupBy(t => new
                {
                    t.CategoryId,
                    t.Category.Name,
                    t.Category.Icon,
                    t.Category.Color,
                    t.Type
                })
                .Select(g =>
                {
                    var typeTotal = g.Key.Type == TransactionType.Expense ? totalExpenses : totalIncome;
                    var amount = g.Sum(t => t.Amount);
                    var baseTotal = typeTotal > 0 ? typeTotal : 1;

                    // Agrupa subcategorias dentro da categoria
                    var subcategories = g
                        .Where(t => t.SubcategoryId.HasValue) // Modificado: Guid? usa HasValue
                        .GroupBy(t => new { t.SubcategoryId, SubcategoryName = t.Subcategory!.Name })
                        .Select(sg => new SubcategoryReportItemDto(
                            SubcategoryId: sg.Key.SubcategoryId!.Value,
                            SubcategoryName: sg.Key.SubcategoryName,
                            Amount: sg.Sum(t => t.Amount),
                            Percentage: Math.Round(sg.Sum(t => t.Amount) / (amount > 0 ? amount : 1) * 100, 2),
                            TransactionCount: sg.Count()))
                        .OrderByDescending(s => s.Amount)
                        .ToList();

                    return new CategoryReportItemDto(
                        CategoryId: g.Key.CategoryId,
                        CategoryName: g.Key.Name,
                        CategoryIcon: g.Key.Icon,
                        CategoryColor: g.Key.Color,
                        Type: g.Key.Type,
                        Amount: amount,
                        Percentage: Math.Round(amount / baseTotal * 100, 2),
                        TransactionCount: g.Count(),
                        Subcategories: subcategories);
                })
                .OrderByDescending(c => c.Amount)
                .ToList();

            return new ReportByCategoryDto(
                From: request.From,
                To: request.To,
                TotalExpenses: totalExpenses,
                TotalIncome: totalIncome,
                Categories: categories);

        }, CacheTtl, cancellationToken);
    }
}
