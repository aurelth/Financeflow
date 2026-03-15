using FinanceFlow.Domain.Entities;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FinanceFlow.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(
        FinanceFlowDbContext context,
        ILogger logger)
    {
        try
        {
            // Aplica migrations pendentes automaticamente
            await context.Database.MigrateAsync();

            await SeedCategoriesAsync(context, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao executar o seed do banco de dados.");
            throw;
        }
    }

    private static async Task SeedCategoriesAsync(
        FinanceFlowDbContext context,
        ILogger logger)
    {
        // Só executa se não houver categorias padrão
        if (await context.Categories.IgnoreQueryFilters().AnyAsync(c => c.IsDefault == true))
        {
            logger.LogInformation("Seed de categorias ignorado — já existem categorias padrão.");
            return;
        }

        logger.LogInformation("Executando seed de categorias padrão...");

        var categories = new List<Category>
        {
            // Despesas
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Alimentação",
                Icon      = "utensils",
                Color     = "#f97316",
                Type      = TransactionType.Expense,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Transporte",
                Icon      = "car",
                Color     = "#3b82f6",
                Type      = TransactionType.Expense,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Saúde",
                Icon      = "heart-pulse",
                Color     = "#ef4444",
                Type      = TransactionType.Expense,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Moradia",
                Icon      = "house",
                Color     = "#8b5cf6",
                Type      = TransactionType.Expense,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Educação",
                Icon      = "graduation-cap",
                Color     = "#06b6d4",
                Type      = TransactionType.Expense,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Lazer",
                Icon      = "gamepad-2",
                Color     = "#ec4899",
                Type      = TransactionType.Expense,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Vestuário",
                Icon      = "shirt",
                Color     = "#f59e0b",
                Type      = TransactionType.Expense,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Tecnologia",
                Icon      = "monitor",
                Color     = "#6366f1",
                Type      = TransactionType.Expense,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Outros",
                Icon      = "ellipsis",
                Color     = "#6b7280",
                Type      = TransactionType.Expense,
                IsDefault = true,
                UserId    = null
            },

            // Receitas
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Salário",
                Icon      = "briefcase",
                Color     = "#22c55e",
                Type      = TransactionType.Income,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Freelance",
                Icon      = "laptop",
                Color     = "#10b981",
                Type      = TransactionType.Income,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Investimentos",
                Icon      = "trending-up",
                Color     = "#14b8a6",
                Type      = TransactionType.Income,
                IsDefault = true,
                UserId    = null
            },
            new() {
                Id        = Guid.NewGuid(),
                Name      = "Outros",
                Icon      = "ellipsis",
                Color     = "#6b7280",
                Type      = TransactionType.Income,
                IsDefault = true,
                UserId    = null
            },
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seed concluído — {Count} categorias padrão criadas.",
            categories.Count);
    }
}
