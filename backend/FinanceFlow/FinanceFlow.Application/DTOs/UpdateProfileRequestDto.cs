namespace FinanceFlow.Application.DTOs;

public record UpdateProfileRequestDto(
    string Name,
    string Currency,
    string Timezone
);
