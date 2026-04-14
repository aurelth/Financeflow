using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.Reports;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using System.Text.Json;

namespace FinanceFlow.Application.UseCases.Analytics.Queries.GetReportByTag;

public class GetReportByTagQueryHandler(
    ITransactionRepository transactionRepository,
    ICacheService cache)
    : IRequestHandler<GetReportByTagQuery, ReportByTagDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    public async Task<ReportByTagDto> Handle(
        GetReportByTagQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"analytics:by-tag:{request.UserId}:{request.From:yyyyMMdd}:{request.To:yyyyMMdd}";

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
                type: null,
                status: null,
                amountMin: null,
                amountMax: null,
                search: null,
                cancellationToken: cancellationToken);

            var confirmed = transactions
                .Where(t => t.Status != TransactionStatus.Scheduled)
                .ToList();

            // Deserializa Tags de JSON antes de usar
            var tagGroups = confirmed
                .Select(t => new
                {
                    Tags = JsonSerializer.Deserialize<string[]>(
                        t.Tags, (JsonSerializerOptions?)null) ?? [],
                    t.Amount,
                    t.Type
                })
                .Where(t => t.Tags.Length > 0) // Modificado: usa .Length para arrays
                .SelectMany(t => t.Tags.Select(tag => new { Tag = tag, t.Amount, t.Type }))
                .GroupBy(x => x.Tag.ToLowerInvariant().Trim())
                .ToList();

            var totalAmount = tagGroups.SelectMany(g => g).Sum(x => x.Amount);
            var baseTotal = totalAmount > 0 ? totalAmount : 1;

            var tags = tagGroups
                .Select(g => new TagReportItemDto(
                    Tag: g.Key,
                    Amount: g.Sum(x => x.Amount),
                    Percentage: Math.Round(g.Sum(x => x.Amount) / baseTotal * 100, 2),
                    TransactionCount: g.Count()))
                .OrderByDescending(t => t.Amount)
                .ToList();

            return new ReportByTagDto(
                From: request.From,
                To: request.To,
                TotalAmount: totalAmount,
                Tags: tags);

        }, CacheTtl, cancellationToken);
    }
}
