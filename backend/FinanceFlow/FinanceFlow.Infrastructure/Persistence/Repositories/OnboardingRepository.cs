using FinanceFlow.Domain.Interfaces;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace FinanceFlow.Infrastructure.Persistence.Repositories;

public class OnboardingRepository(FinanceFlowDbContext context) : IOnboardingRepository
{
    // Quatro consultas de existência (AnyAsync vira EXISTS, para na primeira linha).
    // Sequenciais: o DbContext não aceita consultas paralelas. Soft delete já vem do filtro global.
    public async Task<OnboardingProgress> GetProgressAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var hasTransaction = await context.Transactions
            .AnyAsync(t => t.UserId == userId, cancellationToken);

        // Importação só conta depois de confirmada (Imported > 0)
        var hasConfirmedImport = await context.BankImports
            .AnyAsync(i => i.UserId == userId && i.Imported > 0, cancellationToken);

        var hasBudget = await context.Budgets
            .AnyAsync(b => b.UserId == userId, cancellationToken);

        var hasGoal = await context.Goals
            .AnyAsync(g => g.UserId == userId, cancellationToken);

        return new OnboardingProgress(hasTransaction, hasConfirmedImport, hasBudget, hasGoal);
    }
}
