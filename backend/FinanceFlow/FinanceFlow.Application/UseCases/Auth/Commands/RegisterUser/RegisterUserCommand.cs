using FinanceFlow.Application.DTOs;
using FinanceFlow.Domain.Entities;
using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.RegisterUser;

public record RegisterUserCommand(
    string Name,
    string Email,
    string Password,
    string Cpf,
    Gender Gender,
    string? Currency,
    string? Timezone
) : IRequest<UserProfileDto>;
