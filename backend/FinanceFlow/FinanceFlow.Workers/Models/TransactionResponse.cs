using System.Text.Json.Serialization;

namespace FinanceFlow.Workers.Models;

public record TransactionResponse(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("type")] int Type,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("categoryName")] string CategoryName,
    [property: JsonPropertyName("tags")] string[] Tags
);

public record PagedTransactionResponse(
    [property: JsonPropertyName("items")] IEnumerable<TransactionResponse> Items,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("totalPages")] int TotalPages
);
