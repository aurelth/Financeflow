namespace FinanceFlow.Application.DTOs;

public record AdminUserDto(
    Guid Id,
    string Name,
    string Email,
    string Cpf,
    string Gender,
    string Role,
    string Currency,
    string Timezone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? DeletedAt
);
