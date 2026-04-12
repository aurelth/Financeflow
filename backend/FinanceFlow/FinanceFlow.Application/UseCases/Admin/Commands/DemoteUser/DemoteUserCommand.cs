using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.DemoteUser;

public record DemoteUserCommand(Guid TargetUserId) : IRequest;
