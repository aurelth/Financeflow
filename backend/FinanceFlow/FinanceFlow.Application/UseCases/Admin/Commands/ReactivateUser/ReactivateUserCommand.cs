using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.ReactivateUser;

public record ReactivateUserCommand(Guid TargetUserId) : IRequest;
