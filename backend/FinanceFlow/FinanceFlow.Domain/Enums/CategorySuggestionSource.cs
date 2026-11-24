namespace FinanceFlow.Domain.Enums;

/// <summary>De onde veio a categoria sugerida para uma linha do extrato.</summary>
public enum CategorySuggestionSource
{
    /// <summary>Palavras-chave embutidas (supermercado, uber, farmácia...) sobre as categorias padrão.</summary>
    Keyword = 1,

    /// <summary>Regra aprendida com uma confirmação anterior do próprio usuário.</summary>
    Rule = 2,
}
