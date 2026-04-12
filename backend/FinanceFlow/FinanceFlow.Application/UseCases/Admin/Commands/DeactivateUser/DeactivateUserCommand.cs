using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.DeactivateUser;

public record DeactivateUserCommand(Guid TargetUserId, Guid AdminId) : IRequest;
