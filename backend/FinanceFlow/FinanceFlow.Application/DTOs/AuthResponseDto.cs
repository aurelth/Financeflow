namespace FinanceFlow.Application.DTOs;

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    string TokenType,
    int ExpiresIn,
    UserProfileDto User
);
