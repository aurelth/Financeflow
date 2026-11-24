using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.Common.Mappings;

/// <summary>Mapeamento User → UserProfileDto. Nunca expõe o hash da senha.</summary>
public static class UserMappings
{
    public static UserProfileDto ToProfileDto(this User user) => new(
        Id:        user.Id,
        Name:      user.Name,
        Email:     user.Email,
        Cpf:       user.Cpf,
        Gender:    user.Gender.ToString(),
        Currency:  user.Currency,
        Timezone:  user.Timezone,
        Language:  user.Language,
        CreatedAt: user.CreatedAt,
        Role:      user.Role.ToString(),
        AvatarUrl: user.AvatarPath);
}
