using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Services;

/// <summary>
/// Reduz a descrição de um extrato à sua "identidade": o estabelecimento ou a pessoa.
/// "COMPRA NO DEBITO 12/11 MERCADO EXTRA 1234" e "Compra Cartão 03/12 Mercado Extra" viram "mercado extra".
/// A chave serve para aprender e reaplicar categorias; a descrição gravada no lançamento nunca é alterada
/// (o hash de deduplicação depende dela).
/// </summary>
public static partial class DescriptionNormalizer
{
    /// <summary>Quantas palavras entram na chave: o final da descrição costuma ser cidade, loja ou código.</summary>
    private const int MaxTokens = 5;

    // Palavras que descrevem o meio de pagamento ou a forma jurídica, não quem recebeu
    private static readonly HashSet<string> NoiseWords = new(StringComparer.Ordinal)
    {
        "de", "da", "do", "das", "dos", "e", "em", "no", "na", "nos", "nas", "a", "o", "as", "os",
        "para", "pra", "por", "pelo", "pela", "com", "via", "ref",
        "pix", "ted", "doc", "boleto",
        "compra", "compras", "debito", "deb", "credito", "cred", "cartao", "card",
        "pagamento", "pagto", "pgto", "pag", "parc", "parcela",
        "transferencia", "transf", "enviada", "enviado", "recebida", "recebido",
        "ltda", "sa", "me", "epp", "eireli", "mei",
    };

    // Tokens com dígitos são ruído (códigos, finais de cartão), exceto marcas cujo nome é numérico
    private static readonly HashSet<string> NumericBrands = new(StringComparer.Ordinal) { "99" };

    [GeneratedRegex(@"\b\d{1,2}[/.\-]\d{1,2}(?:[/.\-]\d{2,4})?\b")]
    private static partial Regex DatePattern();

    /// <summary>Chave normalizada, ou vazia quando a descrição não identifica ninguém (ex.: só "PIX ENVIADO").</summary>
    public static string ToKey(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return string.Empty;

        var text = RemoveAccents(description.ToLowerInvariant());
        text = DatePattern().Replace(text, " ");

        var tokens = new List<string>(MaxTokens);
        foreach (var token in Tokenize(text))
        {
            if (!IsMeaningful(token))
                continue;

            tokens.Add(token);
            if (tokens.Count == MaxTokens)
                break;
        }

        var key = string.Join(' ', tokens);
        return key.Length <= CategoryRule.MaxKeyLength ? key : key[..CategoryRule.MaxKeyLength].TrimEnd();
    }

    private static bool IsMeaningful(string token)
    {
        if (NoiseWords.Contains(token))
            return false;

        if (token.Any(char.IsDigit))
            return NumericBrands.Contains(token);

        return token.Length > 1;
    }

    // Letras e dígitos formam palavras; qualquer outra coisa (*, -, /, .) separa
    private static IEnumerable<string> Tokenize(string text)
    {
        var current = new StringBuilder();

        foreach (var c in text)
        {
            if (char.IsLetterOrDigit(c))
            {
                current.Append(c);
            }
            else if (current.Length > 0)
            {
                yield return current.ToString();
                current.Clear();
            }
        }

        if (current.Length > 0)
            yield return current.ToString();
    }

    private static string RemoveAccents(string text)
    {
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString();
    }
}
