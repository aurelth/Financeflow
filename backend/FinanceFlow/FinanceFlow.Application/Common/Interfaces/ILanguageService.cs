namespace FinanceFlow.Application.Common.Interfaces;

public interface ILanguageService
{
    string GetMessage(string key, string? language = null);
    string CurrentLanguage { get; }
}
