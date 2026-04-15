using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinanceFlow.IntegrationTests;

public static class HttpContentExtensions
{
    // Opções de JSON com suporte a enums como string (igual à API)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<T?> ReadAsJsonAsync<T>(this HttpContent content)
        => await JsonSerializer.DeserializeAsync<T>(
            await content.ReadAsStreamAsync(),
            JsonOptions);
}
