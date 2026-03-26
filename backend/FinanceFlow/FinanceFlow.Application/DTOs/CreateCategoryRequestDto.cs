using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs;

public record CreateCategoryRequestDto(
    string Name,
    string Icon,
    string Color,
    TransactionType Type
);
