namespace FinanceFlow.Domain.Interfaces;

/// <summary>Contador diário de uso de IA por usuário.</summary>
public interface IAiUsageRepository
{
    Task<int> GetCountAsync(Guid userId, DateOnly day, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserva uma chamada se o usuário ainda estiver abaixo de <paramref name="limit"/>.
    /// É atômico: requisições simultâneas nunca ultrapassam o limite.
    /// </summary>
    Task<bool> TryConsumeAsync(Guid userId, DateOnly day, int limit, CancellationToken cancellationToken = default);

    /// <summary>Devolve uma chamada reservada que não chegou a ser usada (a IA falhou). Nunca fica negativo.</summary>
    Task ReleaseAsync(Guid userId, DateOnly day, CancellationToken cancellationToken = default);
}
