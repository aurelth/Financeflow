using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs.Admin;

public record CreateDefaultCategoryRequestDto(
    string Name,
    string Icon,
    string Color,
    TransactionType Type
);
