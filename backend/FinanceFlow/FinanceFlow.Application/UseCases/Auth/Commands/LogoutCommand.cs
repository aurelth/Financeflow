using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.Logout;

public record LogoutCommand(Guid UserId) : IRequest;
