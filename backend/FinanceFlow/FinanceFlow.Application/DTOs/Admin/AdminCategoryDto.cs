using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs.Admin;

public record AdminCategoryDto(
    Guid Id,
    string Name,
    string Icon,
    string Color,
    TransactionType Type,
    bool IsActive,
    DateTime CreatedAt
);
