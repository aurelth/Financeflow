using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs.CategoryRules;

/// <param name="Key">Descrição normalizada que dispara a regra (ex.: "mercado extra").</param>
/// <param name="HitCount">Quantas vezes o usuário confirmou esta mesma categoria para a descrição.</param>
public record CategoryRuleDto(
    Guid Id,
    string Key,
    TransactionType Type,
    Guid CategoryId,
    string CategoryName,
    string CategoryIcon,
    string CategoryColor,
    int HitCount,
    DateTime LastUsedAt
);
