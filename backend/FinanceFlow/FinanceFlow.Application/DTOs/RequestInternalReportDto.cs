namespace FinanceFlow.Application.DTOs;

public record RequestInternalReportDto(
    Guid UserId,
    int Month,
    int Year
);
