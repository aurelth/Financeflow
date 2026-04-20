namespace FinanceFlow.Application.Common.Interfaces;

public interface IFinancialContextService
{
    /// <summary>
    /// Recolhe e formata o contexto financeiro do utilizador em texto estruturado
    /// para ser usado como parte do prompt do assistente IA.
    /// </summary>
    Task<string> BuildContextAsync(Guid userId, CancellationToken cancellationToken = default);
}
