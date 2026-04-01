using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using FinanceFlow.Workers.Models;

namespace FinanceFlow.Workers.Services;

public class ReportGeneratorService(
    IHttpClientFactory httpClientFactory,
    ApiAuthService authService,
    IConfiguration configuration,
    ILogger<ReportGeneratorService> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<string?> GenerateAsync(
        Guid reportId,
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken)
    {
        try
        {
            // 1. Atualiza status para Processing
            await UpdateStatusAsync(reportId, "Processing", null, null, cancellationToken);

            // 2. Busca transações do período
            var transactions = await FetchTransactionsAsync(
                userId, month, year, cancellationToken);

            // 3. Gera CSV
            var filePath = await SaveCsvAsync(userId, month, year, transactions, cancellationToken);

            // 4. Atualiza status para Completed
            var fileName = Path.GetFileName(filePath);
            await UpdateStatusAsync(reportId, "Completed", filePath, fileName, cancellationToken);

            // 5. Notifica via SignalR
            await NotifyAsync(userId, reportId, fileName, cancellationToken);

            return filePath;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao gerar relatório {ReportId}", reportId);
            await UpdateStatusAsync(reportId, "Failed", null, null, cancellationToken);
            return null;
        }
    }

    private async Task<List<TransactionResponse>> FetchTransactionsAsync(
        Guid userId,
        int month,
        int year,
        CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("FinanceFlowApi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var dateFrom = new DateTime(year, month, 1).ToString("yyyy-MM-dd");
        var dateTo = new DateTime(year, month, DateTime.DaysInMonth(year, month)).ToString("yyyy-MM-dd");

        var allItems = new List<TransactionResponse>();
        var page = 1;
        var totalPages = 1;

        do
        {
            var url = $"api/transactions/internal?page={page}&pageSize=100&dateFrom={dateFrom}&dateTo={dateTo}&userId={userId}";
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var paged = await response.Content
                .ReadFromJsonAsync<PagedTransactionResponse>(JsonOptions, cancellationToken);

            if (paged is null) break;

            allItems.AddRange(paged.Items);
            totalPages = paged.TotalPages;
            page++;

        } while (page <= totalPages);

        return allItems;
    }

    private static async Task<string> SaveCsvAsync(
        Guid userId,
        int month,
        int year,
        List<TransactionResponse> transactions,
        CancellationToken cancellationToken)
    {
        var solutionDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var dir = Path.Combine(solutionDir, "storage", "reports", userId.ToString());
        Directory.CreateDirectory(dir);

        var fileName = $"relatorio_{year}_{month:00}_{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
        var filePath = Path.Combine(dir, fileName);

        var rows = transactions.Select(t => new ReportCsvRow
        {
            Date = DateTime.Parse(t.Date).ToString("dd/MM/yyyy"),
            Description = t.Description,
            Type = t.Type == 1 ? "Receita" : "Despesa",
            CategoryName = t.CategoryName,
            Status = t.Status switch { 1 => "Pago", 2 => "Pendente", 3 => "Agendado", _ => "" },
            Amount = t.Amount,
            Tags = string.Join(", ", t.Tags),
        }).ToList();

        var config = new CsvConfiguration(new CultureInfo("pt-BR"))
        {
            Delimiter = ";",
            Encoding = Encoding.UTF8,
        };

        await using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
        await using var csv = new CsvWriter(writer, config);

        await csv.WriteRecordsAsync(rows, cancellationToken);

        return filePath;
    }

    private async Task UpdateStatusAsync(
        Guid reportId,
        string status,
        string? filePath,
        string? fileName,
        CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("FinanceFlowApi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var payload = JsonSerializer.Serialize(new { status, filePath, fileName });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PutAsync(
            $"api/reports/{reportId}/status", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            logger.LogWarning("Falha ao atualizar status do relatório {ReportId}: {Status}",
                reportId, response.StatusCode);
    }

    private async Task NotifyAsync(
        Guid userId,
        Guid reportId,
        string? fileName,
        CancellationToken cancellationToken)
    {
        var token = await authService.GetTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("FinanceFlowApi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var payload = JsonSerializer.Serialize(new { userId, reportId, fileName });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            "api/reports/notify", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
            logger.LogWarning("Falha ao notificar relatório {ReportId}: {Status}",
                reportId, response.StatusCode);
    }
}
