using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface ITokenService
{
    /// <summary>
    /// Gera um Access Token JWT para o utilizador.
    /// Expira em 15 minutos.
    /// </summary>
    string GenerateAccessToken(User user);

    /// <summary>
    /// Gera um Access Token JWT para um serviço interno (worker).
    /// Expira em 60 minutos.
    /// </summary>
    (string AccessToken, DateTime ExpiresAt) GenerateServiceToken(Guid serviceUserId, string serviceName);

    /// <summary>
    /// Gera um Refresh Token opaco (GUID).
    /// Expira em 7 dias — armazenado no Redis.
    /// </summary>
    string GenerateRefreshToken();

    /// <summary>
    /// Extrai o UserId do Access Token mesmo que esteja expirado.
    /// Usado no fluxo de renovação do token.
    /// </summary>
    Guid? GetUserIdFromExpiredToken(string accessToken);
}
