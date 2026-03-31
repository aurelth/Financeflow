using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class ReportRepository(FinanceFlowDbContext context) : IReportRepository
{
    public async Task<IEnumerable<Report>> GetByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Reports
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Report?> GetByIdAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await context.Reports
            .Where(r => r.Id == id && r.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Report?> GetByIdInternalAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        await context.Reports
            .Where(r => r.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(
        Report report,
        CancellationToken cancellationToken = default)
    {
        await context.Reports.AddAsync(report, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(
        Report report,
        CancellationToken cancellationToken = default)
    {
        context.Reports.Update(report);
        await context.SaveChangesAsync(cancellationToken);
    }
}
