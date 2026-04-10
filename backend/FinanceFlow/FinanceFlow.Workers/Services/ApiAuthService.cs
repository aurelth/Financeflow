using System.Net.Http.Json;
using FinanceFlow.Workers.Models;

namespace FinanceFlow.Workers.Services;

public class ApiAuthService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ApiAuthService> logger)
{
    private string? _cachedToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiresAt.AddMinutes(-5))
            return _cachedToken;

        logger.LogInformation("A renovar JWT de serviço...");

        // Retry com backoff exponencial para aguardar a API arrancar
        var maxAttempts = 5;
        var delay = TimeSpan.FromSeconds(3);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var client = httpClientFactory.CreateClient("FinanceFlowApi");
                var serviceKey = configuration["ServiceAuth:ServiceKey"]!;

                var response = await client.PostAsJsonAsync(
                    "api/auth/service-token",
                    new { ServiceKey = serviceKey },
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var result = await response.Content
                    .ReadFromJsonAsync<ServiceTokenResponse>(cancellationToken: cancellationToken)
                    ?? throw new InvalidOperationException("Resposta de token inválida.");

                _cachedToken = result.AccessToken;
                _tokenExpiresAt = result.ExpiresAt;

                logger.LogInformation("JWT de serviço renovado. Expira em {ExpiresAt}", _tokenExpiresAt);

                return _cachedToken;
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    "Tentativa {Attempt}/{Max} de obter token falhou: {Message}. Aguardando {Delay}s...",
                    attempt, maxAttempts, ex.Message, delay.TotalSeconds);

                await Task.Delay(delay, cancellationToken);
                delay *= 2; // backoff exponencial: 3s, 6s, 12s, 24s
            }
        }

        throw new InvalidOperationException(
            "Não foi possível obter token de serviço após várias tentativas.");
    }
}
