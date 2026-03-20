using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.Logout;

public class LogoutCommandHandler(
    IRefreshTokenService refreshTokenService)
    : IRequestHandler<LogoutCommand>
{
    public async Task Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        await refreshTokenService.RevokeAsync(request.UserId, cancellationToken);
    }
}
