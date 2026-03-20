namespace FinanceFlow.Application.DTOs;

public record UserProfileDto(
    Guid Id,
    string Name,
    string Email,
    string Currency,
    string Timezone,
    DateTime CreatedAt
);
