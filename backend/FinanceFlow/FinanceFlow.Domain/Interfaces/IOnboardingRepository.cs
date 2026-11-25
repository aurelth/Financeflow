namespace FinanceFlow.Domain.Interfaces;

/// <summary>O que o usuário já fez no app (base do checklist de primeiros passos).</summary>
public record OnboardingProgress(
    bool HasTransaction,
    bool HasConfirmedImport,
    bool HasBudget,
    bool HasGoal);

public interface IOnboardingRepository
{
    /// <summary>Consulta leve (apenas existência) do que o usuário já fez.</summary>
    Task<OnboardingProgress> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default);
}
