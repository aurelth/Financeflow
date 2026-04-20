using System.Text;
using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Services;

public class FinancialContextService(
    ITransactionRepository transactionRepository,
    IBudgetRepository budgetRepository,
    ILogger<FinancialContextService> logger) : IFinancialContextService
{
    public async Task<string> BuildContextAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var month = now.Month;
        var year = now.Year;

        logger.LogInformation("Construindo contexto financeiro para utilizador {UserId}.", userId);

        // Execução sequencial — EF Core não suporta queries paralelas no mesmo DbContext
        var (totalIncome, totalExpense, lastTen) =
            await transactionRepository.GetMonthlySummaryAsync(userId, month, year, cancellationToken);

        var topCategories =
            await transactionRepository.GetTopExpenseCategoriesAsync(userId, month, year, 5, cancellationToken);

        var budgets =
            await budgetRepository.GetByUserAndPeriodAsync(userId, month, year, cancellationToken);

        var balance = totalIncome - totalExpense;
        var monthName = new DateTime(year, month, 1).ToString("MMMM", new System.Globalization.CultureInfo("pt-BR"));

        var sb = new StringBuilder();
        sb.AppendLine($"=== CONTEXTO FINANCEIRO — {monthName.ToUpper()} DE {year} ===");
        sb.AppendLine();

        // Resumo do mês
        sb.AppendLine("## RESUMO DO MÊS");
        sb.AppendLine($"- Receitas: R$ {totalIncome:N2}");
        sb.AppendLine($"- Despesas: R$ {totalExpense:N2}");
        sb.AppendLine($"- Saldo: R$ {balance:N2}");
        sb.AppendLine();

        // Top categorias
        sb.AppendLine("## TOP 5 CATEGORIAS DE DESPESA");
        var categoriesList = topCategories.ToList();
        if (categoriesList.Count == 0)
        {
            sb.AppendLine("- Nenhuma despesa registada este mês.");
        }
        else
        {
            foreach (var (categoryName, totalAmount) in categoriesList)
                sb.AppendLine($"- {categoryName}: R$ {totalAmount:N2}");
        }
        sb.AppendLine();

        // Orçamentos
        sb.AppendLine("## ORÇAMENTOS DO MÊS");
        var budgetsList = budgets.ToList();
        if (budgetsList.Count == 0)
        {
            sb.AppendLine("- Nenhum orçamento configurado.");
        }
        else
        {
            foreach (var budget in budgetsList)
            {
                var spent = categoriesList
                    .FirstOrDefault(c => c.CategoryName == budget.Category?.Name)
                    .TotalAmount;
                var percentage = budget.LimitAmount > 0
                    ? (spent / budget.LimitAmount) * 100
                    : 0;
                var status = percentage >= 100 ? "EXCEDIDO" : percentage >= 80 ? "ATENÇÃO" : "OK";
                sb.AppendLine($"- {budget.Category?.Name}: R$ {spent:N2} / R$ {budget.LimitAmount:N2} ({percentage:N0}%) [{status}]");
            }
        }
        sb.AppendLine();

        // Últimas transações
        sb.AppendLine("## ÚLTIMAS 10 TRANSAÇÕES");
        var lastTenList = lastTen.ToList();
        if (lastTenList.Count == 0)
        {
            sb.AppendLine("- Nenhuma transação registada este mês.");
        }
        else
        {
            foreach (var t in lastTenList)
            {
                var tipo = t.Type == TransactionType.Income ? "Receita" : "Despesa";
                sb.AppendLine($"- {t.Date:dd/MM}: [{tipo}] {t.Description} — R$ {t.Amount:N2} ({t.Category?.Name})");
            }
        }

        return sb.ToString();
    }
}
