namespace FinanceFlow.Application.DTOs;

public record AdminMetricsDto(
    int TotalUsers,
    int ActiveUsers,
    int InactiveUsers,
    int TotalAdmins,
    int TotalCategories,
    int DefaultCategories
);
