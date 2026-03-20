using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.LoginUser;

public record LoginUserCommand(
    string Email,
    string Password
) : IRequest<AuthResponseDto>;
