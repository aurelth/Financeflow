namespace FinanceFlow.Application.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    UserProfileDto User
);
