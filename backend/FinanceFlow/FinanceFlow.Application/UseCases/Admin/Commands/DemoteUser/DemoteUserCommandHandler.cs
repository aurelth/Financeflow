using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.DemoteUser;

public class DemoteUserCommandHandler(
    IUserRepository userRepository
) : IRequestHandler<DemoteUserCommand>
{
    public async Task Handle(
        DemoteUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.TargetUserId, cancellationToken)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (user.Role != UserRole.Admin)
            throw new ValidationException(
                "Este usuário não é Admin.",
                new Dictionary<string, string[]>
                {
                    { "UserId", ["Este usuário não é Admin."] }
                });

        // Não permite rebaixar o último Admin
        var activeAdmins = await userRepository.CountActiveAdminsAsync(cancellationToken);
        if (activeAdmins <= 1)
            throw new ValidationException(
                "Não é possível rebaixar o único Admin ativo do sistema.",
                new Dictionary<string, string[]>
                {
                    { "UserId", ["Não é possível rebaixar o único Admin ativo do sistema."] }
                });

        user.Role = UserRole.User;
        await userRepository.UpdateAsync(user, cancellationToken);
    }
}
