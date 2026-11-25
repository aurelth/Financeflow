namespace FinanceFlow.Application.DTOs.Onboarding;

/// <summary>Chaves estáveis dos passos do checklist (o frontend traduz e liga cada uma a uma tela).</summary>
public static class OnboardingSteps
{
    public const string Transaction = "transaction";
    public const string Import = "import";
    public const string Budget = "budget";
    public const string Goal = "goal";
}

public record OnboardingStepDto(string Key, bool Done);

public record OnboardingChecklistDto(
    IReadOnlyList<OnboardingStepDto> Steps,
    int Completed,
    int Total,
    bool Dismissed);

public record SetChecklistDismissedRequestDto(bool Dismissed);
