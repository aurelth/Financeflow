namespace FinanceFlow.Domain.Interfaces;

public interface IRefreshTokenService
{
    /// <summary>
    /// Armazena o Refresh Token no Redis associado ao UserId.
    /// TTL de 7 dias.
    /// </summary>
    Task SaveAsync(Guid userId, string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Valida se o Refresh Token existe no Redis para o UserId.
    /// </summary>
    Task<bool> ValidateAsync(Guid userId, string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove o Refresh Token do Redis (logout).
    /// </summary>
    Task RevokeAsync(Guid userId, CancellationToken cancellationToken = default);
}
