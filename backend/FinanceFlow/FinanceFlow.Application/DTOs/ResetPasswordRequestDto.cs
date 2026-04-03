namespace FinanceFlow.Application.DTOs;

public record ResetPasswordRequestDto(
    string Token,
    string NewPassword,
    string ConfirmPassword);
