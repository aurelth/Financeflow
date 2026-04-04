namespace FinanceFlow.Application.DTOs;

public record UserProfileDto(
    Guid Id,
    string Name,
    string Email,
    string Cpf,
    string Gender,
    string Currency,
    string Timezone,
    DateTime CreatedAt
);
