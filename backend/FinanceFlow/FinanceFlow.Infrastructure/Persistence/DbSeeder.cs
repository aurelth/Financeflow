using FinanceFlow.Domain.Constants;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FinanceFlow.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(
        FinanceFlowDbContext context,
        ILogger logger,
        IConfiguration configuration)
    {
        try
        {
            await context.Database.MigrateAsync();
            await SeedCategoriesAsync(context, logger);
            await SeedAdminAsync(context, logger, configuration);
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
        if (await context.Categories.IgnoreQueryFilters().AnyAsync(c => c.IsDefault == true))
        {
            logger.LogInformation("Seed de categorias ignorado — já existem categorias padrão.");
            return;
        }

        logger.LogInformation("Executando seed de categorias padrão...");

        var categories = new List<Category>
        {
            // Despesas
            new() { Id = Guid.NewGuid(), Name = "Alimentação",   Icon = "utensils",         Color = "#f97316", Type = TransactionType.Expense,  IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Transporte",    Icon = "car",              Color = "#3b82f6", Type = TransactionType.Expense,  IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Saúde",         Icon = "heart-pulse",      Color = "#ef4444", Type = TransactionType.Expense,  IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Moradia",       Icon = "house",            Color = "#8b5cf6", Type = TransactionType.Expense,  IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Educação",      Icon = "graduation-cap",   Color = "#06b6d4", Type = TransactionType.Expense,  IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Lazer",         Icon = "gamepad-2",        Color = "#ec4899", Type = TransactionType.Expense,  IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Vestuário",     Icon = "shirt",            Color = "#f59e0b", Type = TransactionType.Expense,  IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Tecnologia",    Icon = "monitor",          Color = "#6366f1", Type = TransactionType.Expense,  IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Outros",        Icon = "ellipsis",         Color = "#6b7280", Type = TransactionType.Expense,  IsDefault = true, UserId = null },
            // Receitas
            new() { Id = Guid.NewGuid(), Name = "Salário",       Icon = "briefcase",        Color = "#22c55e", Type = TransactionType.Income,   IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Freelance",     Icon = "laptop",           Color = "#10b981", Type = TransactionType.Income,   IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Investimentos", Icon = "trending-up",      Color = "#14b8a6", Type = TransactionType.Income,   IsDefault = true, UserId = null },
            new() { Id = Guid.NewGuid(), Name = "Outros",        Icon = "ellipsis",         Color = "#6b7280", Type = TransactionType.Income,   IsDefault = true, UserId = null },
            // Categoria padrão de transferência com GUID fixo
            new() { Id = WellKnownIds.TransferCategoryId, Name = "Transferência", Icon = "arrow-left-right", Color = "#818cf8", Type = TransactionType.Transfer, IsDefault = true, UserId = null },
        };

        await context.Categories.AddRangeAsync(categories);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "Seed concluído — {Count} categorias padrão criadas.", categories.Count);
    }

    private static async Task SeedAdminAsync(
        FinanceFlowDbContext context,
        ILogger logger,
        IConfiguration configuration)
    {
        var adminExists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Role == UserRole.Admin && u.DeletedAt == null);

        if (adminExists)
        {
            logger.LogInformation("Seed de Admin ignorado — já existe um Admin ativo.");
            return;
        }

        logger.LogInformation("Executando seed do Admin padrão...");

        var adminConfig = configuration.GetSection("AdminSeed");
        var email = adminConfig["Email"] ?? "admin@financeflow.com";
        var password = adminConfig["Password"] ?? "Admin@123";
        var name = adminConfig["Name"] ?? "Administrador";

        var existingUserId = await context.Users
            .IgnoreQueryFilters()
            .Where(u => u.Email == email && u.DeletedAt == null)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        if (existingUserId != Guid.Empty)
        {
            var user = await context.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Id == existingUserId);

            if (user is not null)
            {
                user.Role = UserRole.Admin;
                user.DeletedAt = null;
                await context.SaveChangesAsync();
            }

            logger.LogInformation("Usuário existente promovido a Admin: {Email}", email);
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, 12);

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Cpf = string.Empty,
            Gender = Gender.Male,
            Currency = "BRL",
            Timezone = "America/Sao_Paulo",
            Role = UserRole.Admin,
            CreatedAt = DateTime.UtcNow,
            NotificationPreferences = new UserNotificationPreferences
            {
                BudgetWarningEnabled = true,
                BudgetCriticalEnabled = true,
                TransactionDueTomorrowEnabled = true,
                TransactionDueIn3DaysEnabled = true,
            }
        };

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();

        logger.LogInformation("Admin padrão criado: {Email}", email);
    }
}
