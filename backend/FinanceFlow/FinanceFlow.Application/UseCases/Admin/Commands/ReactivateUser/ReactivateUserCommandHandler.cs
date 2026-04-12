using FinanceFlow.Application.Common.Exceptions;
using FinanceFlow.Domain.Interfaces;
using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.ReactivateUser;

public class ReactivateUserCommandHandler(
    IUserRepository userRepository
) : IRequestHandler<ReactivateUserCommand>
{
    public async Task Handle(
        ReactivateUserCommand request,
        CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.TargetUserId, cancellationToken)
            ?? throw new NotFoundException("Usuário não encontrado.");

        if (user.DeletedAt == null)
            throw new ValidationException(
                "Este usuário já está ativo.",
                new Dictionary<string, string[]>
                {
                    { "UserId", ["Este usuário já está ativo."] }
                });

        await userRepository.ReactivateAsync(request.TargetUserId, cancellationToken);
    }
}
