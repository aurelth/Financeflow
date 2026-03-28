namespace FinanceFlow.Application.DTOs;

public record PagedResultDto<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
