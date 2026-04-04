namespace FinanceFlow.Application.DTOs;

public record RegisterRequestDto(
    string Name,
    string Email,
    string Password,
    string Cpf,
    string Gender,
    string? Currency,
    string? Timezone
);
