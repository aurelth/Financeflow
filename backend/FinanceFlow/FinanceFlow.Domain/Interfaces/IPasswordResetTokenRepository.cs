using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Domain.Interfaces;

public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Retorna um token válido (não expirado e não usado) pelo valor do token.
    /// </summary>
    Task<PasswordResetToken?> GetValidTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalida todos os tokens anteriores do usuário antes de gerar um novo.
    /// </summary>
    Task InvalidateUserTokensAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        PasswordResetToken token,
        CancellationToken cancellationToken = default);
}
