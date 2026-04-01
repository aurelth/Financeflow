using System.Text.Json.Serialization;

namespace FinanceFlow.Workers.Models;

public record ReportRequestedEvent(
    [property: JsonPropertyName("reportId")] Guid ReportId,
    [property: JsonPropertyName("userId")] Guid UserId,
    [property: JsonPropertyName("month")] int Month,
    [property: JsonPropertyName("year")] int Year,
    [property: JsonPropertyName("requestedAt")] DateTime RequestedAt
);
