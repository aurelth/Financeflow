namespace FinanceFlow.Application.DTOs;

public record UpdateCategoryRequestDto(
    string Name,
    string Icon,
    string Color
);
