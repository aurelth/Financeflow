using System.Globalization;
using System.Text;
using System.Text.Json;
using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Services.Ai;

/// <summary>Um lançamento extraído e já saneado, antes de virar DTO.</summary>
public record ParsedEntry(
    TransactionType Type,
    decimal Amount,
    DateOnly Date,
    string Description,
    Guid? CategoryId);

/// <summary>
/// Transforma a resposta da IA em lançamentos confiáveis. A saída do modelo é entrada não confiável
/// (pode vir truncada, com cerca de código, com valores absurdos ou induzida por texto da imagem),
/// então tudo é revalidado aqui: o que não passa vira "sem categoria" ou volta ao padrão, nunca quebra.
/// </summary>
public static class QuickEntryReplyParser
{
    public const int MaxDescriptionLength = 100;
    public const decimal MaxAmount = 100_000_000m;

    public const string UnreadableMessage =
        "Não consegui entender o resultado da IA. Tente de novo ou use o formulário.";

    public static IReadOnlyList<ParsedEntry> Parse(
        string reply,
        DateOnly today,
        IReadOnlyCollection<Category> categories,
        int maxEntries)
    {
        var json = ExtractJsonObject(reply)
            ?? throw new ValidationException(UnreadableMessage);

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            throw new ValidationException(UnreadableMessage);
        }

        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty("entries", out var entries) ||
                entries.ValueKind != JsonValueKind.Array)
                throw new ValidationException(UnreadableMessage);

            var byId = categories.ToDictionary(c => c.Id);
            var result = new List<ParsedEntry>();

            foreach (var entry in entries.EnumerateArray())
            {
                if (result.Count >= maxEntries)
                    break;

                if (entry.ValueKind != JsonValueKind.Object)
                    continue;

                var parsed = ParseEntry(entry, today, byId);
                if (parsed is not null)
                    result.Add(parsed);
            }

            return result;
        }
    }

    private static ParsedEntry? ParseEntry(
        JsonElement entry,
        DateOnly today,
        Dictionary<Guid, Category> categories)
    {
        var type = ReadString(entry, "type")?.Trim().ToLowerInvariant() switch
        {
            "expense" => TransactionType.Expense,
            "income" => TransactionType.Income,
            _ => (TransactionType?)null,
        };

        // Sem tipo ou sem valor positivo não existe lançamento: descarta em vez de chutar
        if (type is null)
            return null;

        var amount = ReadAmount(entry);
        if (amount is null or <= 0 or > MaxAmount)
            return null;

        var description = CleanDescription(ReadString(entry, "description"));
        if (description.Length == 0)
            description = "Lançamento por IA";

        Guid? categoryId = null;
        if (Guid.TryParse(ReadString(entry, "categoryId"), out var id) &&
            categories.TryGetValue(id, out var category) &&
            category.Type == type)
            categoryId = id;

        return new ParsedEntry(type.Value, Math.Round(amount.Value, 2, MidpointRounding.AwayFromZero), ResolveDate(entry, today), description, categoryId);
    }

    // Data fora de uma janela plausível é quase sempre alucinação (ano errado): usa hoje e o usuário confere
    private static DateOnly ResolveDate(JsonElement entry, DateOnly today)
    {
        if (DateOnly.TryParseExact(ReadString(entry, "date"), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var date) &&
            date >= today.AddYears(-5) &&
            date <= today.AddYears(1))
            return date;

        return today;
    }

    private static decimal? ReadAmount(JsonElement entry)
    {
        if (!entry.TryGetProperty("amount", out var element))
            return null;

        switch (element.ValueKind)
        {
            case JsonValueKind.Number when element.TryGetDecimal(out var number):
                return number;
            case JsonValueKind.String:
                // O modelo às vezes devolve "45,90" ou "R$ 45.90"
                var raw = element.GetString()!
                    .Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase)
                    .Trim()
                    .Replace(',', '.');
                return decimal.TryParse(raw, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var text)
                    ? text
                    : null;
            default:
                return null;
        }
    }

    private static string? ReadString(JsonElement entry, string name) =>
        entry.TryGetProperty(name, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;

    // Remove caracteres de controle e espaços repetidos; limita o tamanho
    private static string CleanDescription(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
            sb.Append(char.IsControl(c) ? ' ' : c);

        var clean = string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return clean.Length <= MaxDescriptionLength ? clean : clean[..MaxDescriptionLength].TrimEnd();
    }

    // Aceita a resposta limpa ou embrulhada em cerca de código / com texto em volta
    private static string? ExtractJsonObject(string reply)
    {
        var start = reply.IndexOf('{');
        var end = reply.LastIndexOf('}');
        return start >= 0 && end > start ? reply[start..(end + 1)] : null;
    }
}
