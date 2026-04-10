using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Settings.Commands.DeleteAccount;

public class DeleteAccountCommandHandler(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IRefreshTokenService refreshTokenService
) : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(
        DeleteAccountCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("Utilizador não encontrado.");

        // Verifica senha antes de excluir
        if (!passwordService.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException(
                "Senha incorreta.",
                new Dictionary<string, string[]>
                {
                    { "CurrentPassword", ["Senha incorreta."] }
                });

        // Revoga todos os tokens activos
        await refreshTokenService.RevokeAsync(request.UserId, cancellationToken);

        // Soft-delete do utilizador
        await userRepository.DeleteAsync(request.UserId, cancellationToken);
    }
}
