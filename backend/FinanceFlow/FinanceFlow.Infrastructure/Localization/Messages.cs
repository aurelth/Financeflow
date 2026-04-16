namespace FinanceFlow.Infrastructure.Localization;

public static class Messages
{
    // Chaves de mensagens
    public const string Required = "required";
    public const string MinLength = "min_length";
    public const string MaxLength = "max_length";
    public const string InvalidEmail = "invalid_email";
    public const string InvalidColor = "invalid_color";
    public const string InvalidEnum = "invalid_enum";
    public const string InvalidLanguage = "invalid_language";
    public const string InvalidCurrency = "invalid_currency";
    public const string NotFound = "not_found";
    public const string Unauthorized = "unauthorized";
    public const string InternalError = "internal_error";
    public const string ValidationError = "validation_error";

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["pt-BR"] = new()
        {
            [Required] = "{0} é obrigatório.",
            [MinLength] = "{0} deve ter pelo menos {1} caracteres.",
            [MaxLength] = "{0} deve ter no máximo {1} caracteres.",
            [InvalidEmail] = "Email inválido.",
            [InvalidColor] = "A cor deve ser um valor hexadecimal válido (ex: #6366f1).",
            [InvalidEnum] = "Valor inválido para {0}.",
            [InvalidLanguage] = "Idioma não suportado. Use: pt-BR, en-US, es-ES ou fr-FR.",
            [InvalidCurrency] = "Moeda inválida.",
            [NotFound] = "{0} não encontrado.",
            [Unauthorized] = "Não autorizado.",
            [InternalError] = "Ocorreu um erro interno. Tente novamente.",
            [ValidationError] = "Um ou mais erros de validação ocorreram.",
        },
        ["en-US"] = new()
        {
            [Required] = "{0} is required.",
            [MinLength] = "{0} must be at least {1} characters.",
            [MaxLength] = "{0} must be at most {1} characters.",
            [InvalidEmail] = "Invalid email.",
            [InvalidColor] = "Color must be a valid hexadecimal value (e.g. #6366f1).",
            [InvalidEnum] = "Invalid value for {0}.",
            [InvalidLanguage] = "Unsupported language. Use: pt-BR, en-US, es-ES or fr-FR.",
            [InvalidCurrency] = "Invalid currency.",
            [NotFound] = "{0} not found.",
            [Unauthorized] = "Unauthorized.",
            [InternalError] = "An internal error occurred. Please try again.",
            [ValidationError] = "One or more validation errors occurred.",
        },
        ["es-ES"] = new()
        {
            [Required] = "{0} es obligatorio.",
            [MinLength] = "{0} debe tener al menos {1} caracteres.",
            [MaxLength] = "{0} debe tener como máximo {1} caracteres.",
            [InvalidEmail] = "Email inválido.",
            [InvalidColor] = "El color debe ser un valor hexadecimal válido (ej: #6366f1).",
            [InvalidEnum] = "Valor inválido para {0}.",
            [InvalidLanguage] = "Idioma no soportado. Use: pt-BR, en-US, es-ES o fr-FR.",
            [InvalidCurrency] = "Moneda inválida.",
            [NotFound] = "{0} no encontrado.",
            [Unauthorized] = "No autorizado.",
            [InternalError] = "Ocurrió un error interno. Inténtelo de nuevo.",
            [ValidationError] = "Se produjeron uno o más errores de validación.",
        },
        ["fr-FR"] = new()
        {
            [Required] = "{0} est obligatoire.",
            [MinLength] = "{0} doit contenir au moins {1} caractères.",
            [MaxLength] = "{0} doit contenir au maximum {1} caractères.",
            [InvalidEmail] = "Email invalide.",
            [InvalidColor] = "La couleur doit être une valeur hexadécimale valide (ex: #6366f1).",
            [InvalidEnum] = "Valeur invalide pour {0}.",
            [InvalidLanguage] = "Langue non supportée. Utilisez: pt-BR, en-US, es-ES ou fr-FR.",
            [InvalidCurrency] = "Devise invalide.",
            [NotFound] = "{0} introuvable.",
            [Unauthorized] = "Non autorisé.",
            [InternalError] = "Une erreur interne s'est produite. Veuillez réessayer.",
            [ValidationError] = "Une ou plusieurs erreurs de validation se sont produites.",
        },
    };

    public static string Get(string key, string language, params object[] args)
    {
        var lang = Translations.ContainsKey(language) ? language : "en-US";

        if (!Translations[lang].TryGetValue(key, out var template))
            return key;

        return args.Length > 0 ? string.Format(template, args) : template;
    }
}
