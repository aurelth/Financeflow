using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.RegisterUser;

public record RegisterUserCommand(
    string Name,
    string Email,
    string Password,
    string? Currency,
    string? Timezone
) : IRequest<UserProfileDto>;
