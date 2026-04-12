namespace FinanceFlow.Application.DTOs;

public record AdminUserListDto(
    IEnumerable<AdminUserDto> Users,
    int Total,
    int Page,
    int PageSize,
    int TotalPages
);
