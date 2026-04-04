namespace FinanceFlow.Application.DTOs;

public record UpdateProfileRequestDto(    
    string Currency,
    string Timezone
);
