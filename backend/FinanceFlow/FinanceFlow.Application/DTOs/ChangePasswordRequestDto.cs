namespace FinanceFlow.Application.DTOs;

public record ChangePasswordRequestDto(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword
);
