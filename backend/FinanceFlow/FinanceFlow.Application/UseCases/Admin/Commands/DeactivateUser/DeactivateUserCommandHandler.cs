using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.DeactivateUser;

public class DeactivateUserCommandHandler(
    IUserRepository userRepository
) : IRequestHandler<DeactivateUserCommand>
{
    public async Task Handle(
        DeactivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.TargetUserId, cancellationToken)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (user.DeletedAt != null)
            throw new ValidationException(
                "Este usuário já está desativado.",
                new Dictionary<string, string[]>
                {
                    { "UserId", ["Este usuário já está desativado."] }
                });

        // Não permite desativar o último Admin
        if (user.Role == Domain.Enums.UserRole.Admin)
        {
            var activeAdmins = await userRepository.CountActiveAdminsAsync(cancellationToken);
            if (activeAdmins <= 1)
                throw new ValidationException(
                    "Não é possível desativar o único Admin ativo do sistema.",
                    new Dictionary<string, string[]>
                    {
                        { "UserId", ["Não é possível desativar o único Admin ativo do sistema."] }
                    });
        }

        await userRepository.DeleteAsync(request.TargetUserId, cancellationToken);
    }
}
