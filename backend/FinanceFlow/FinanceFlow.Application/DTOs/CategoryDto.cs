using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string Icon,
    string Color,
    TransactionType Type,
    bool IsDefault,
    bool IsActive,
    bool IsOwner,      // true = criada pelo utilizador, false = padrão do sistema
    IEnumerable<SubcategoryDto> Subcategories
);
