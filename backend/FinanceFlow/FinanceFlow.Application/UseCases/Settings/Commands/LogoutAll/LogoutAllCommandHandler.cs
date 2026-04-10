using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Settings.Commands.LogoutAll;

public class LogoutAllCommandHandler(
    IRefreshTokenService refreshTokenService
) : IRequestHandler<LogoutAllCommand>
{
    public async Task Handle(
        LogoutAllCommand request,
        CancellationToken cancellationToken)
    {
        await refreshTokenService.RevokeAsync(request.UserId, cancellationToken);
    }
}
