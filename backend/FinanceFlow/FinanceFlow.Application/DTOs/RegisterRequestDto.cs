namespace FinanceFlow.Application.DTOs;

public record RegisterRequestDto(
    string Name,
    string Email,
    string Password,
    string? Currency,
    string? Timezone
);
