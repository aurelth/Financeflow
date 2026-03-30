namespace FinanceFlow.Application.Common.Interfaces;

public interface ICacheService
{
    /// <summary>
    /// Retorna o valor em cache ou executa a factory e armazena o resultado.
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan ttl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove uma entrada do cache pelo prefixo da chave.
    /// </summary>
    Task RemoveAsync(
        string key,
        CancellationToken cancellationToken = default);
}
