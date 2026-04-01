namespace FinanceFlow.Application.DTOs;

public record NotificationDto(
    Guid Id,
    string Type,
    string Message,
    bool IsRead,
    DateTime CreatedAt
);
