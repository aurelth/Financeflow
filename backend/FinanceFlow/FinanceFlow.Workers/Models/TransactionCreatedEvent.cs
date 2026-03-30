using System.Text.Json.Serialization;

namespace FinanceFlow.Workers.Models;

public record TransactionCreatedEvent(
    [property: JsonPropertyName("transactionId")] Guid TransactionId,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("type")] int Type,
    [property: JsonPropertyName("date")] DateTime Date,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("status")] int Status,
    [property: JsonPropertyName("categoryId")] Guid CategoryId,
    [property: JsonPropertyName("categoryName")] string CategoryName,
    [property: JsonPropertyName("createdAt")] DateTime CreatedAt
);
