using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Enums;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.PromoteUser;

public class PromoteUserCommandHandler(
    IUserRepository userRepository
) : IRequestHandler<PromoteUserCommand>
{
    public async Task Handle(
        PromoteUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.TargetUserId, cancellationToken)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (user.DeletedAt != null)
            throw new ValidationException(
                "Não é possível promover um usuário desativado.",
                new Dictionary<string, string[]>
                {
                    { "UserId", ["Não é possível promover um usuário desativado."] }
                });

        if (user.Role == UserRole.Admin)
            throw new ValidationException(
                "Este usuário já é Admin.",
                new Dictionary<string, string[]>
                {
                    { "UserId", ["Este usuário já é Admin."] }
                });

        user.Role = UserRole.Admin;
        await userRepository.UpdateAsync(user, cancellationToken);
    }
}
