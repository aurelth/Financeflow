using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FinanceFlow.Workers.Models;

namespace FinanceFlow.Workers.Services;

public class BankImportProcessingService(
    IHttpClientFactory httpClientFactory,
    ApiAuthService authService,
    ILogger<BankImportProcessingService> logger)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task ProcessAsync(
        BankImportCreatedEvent importEvent,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processando importação {ImportId} para UserId={UserId}",
            importEvent.ImportId, importEvent.UserId);

        var token = await authService.GetTokenAsync(cancellationToken);
        var client = httpClientFactory.CreateClient("FinanceFlowApi");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        // Notifica a API para processar a importação
        var response = await client.PostAsJsonAsync(
            $"api/imports/{importEvent.ImportId}/process",
            new { importEvent.UserId },
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError(
                "Erro ao processar importação {ImportId}: {Status}",
                importEvent.ImportId, response.StatusCode);
            return;
        }

        logger.LogInformation(
            "Importação {ImportId} processada com sucesso",
            importEvent.ImportId);
    }

    public static string ComputeHash(DateTime date, decimal amount, string description, string type)
    {
        var raw = $"{date:yyyy-MM-dd}|{amount}|{description.Trim().ToUpperInvariant()}|{type}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
