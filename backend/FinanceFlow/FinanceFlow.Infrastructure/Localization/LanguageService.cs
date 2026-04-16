using FinanceFlow.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FinanceFlow.Infrastructure.Localization;

public class LanguageService(IHttpContextAccessor httpContextAccessor) : ILanguageService
{
    private static readonly HashSet<string> Supported = ["pt-BR", "en-US", "es-ES", "fr-FR"];

    public string CurrentLanguage
    {
        get
        {
            var header = httpContextAccessor.HttpContext?
                .Request.Headers["Accept-Language"]
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(header))
                return "en-US";

            // Suporta "pt-BR,pt;q=0.9,en;q=0.8" — pega o primeiro valor
            var primary = header.Split(',')[0].Split(';')[0].Trim();

            return Supported.Contains(primary) ? primary : "en-US";
        }
    }

    public string GetMessage(string key, string? language = null)
        => Messages.Get(key, language ?? CurrentLanguage);
}
