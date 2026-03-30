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
}
