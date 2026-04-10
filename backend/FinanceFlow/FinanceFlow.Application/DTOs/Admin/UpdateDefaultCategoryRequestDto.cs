namespace FinanceFlow.Application.DTOs.Admin;

public record UpdateDefaultCategoryRequestDto(
    string Name,
    string Icon,
    string Color
);
