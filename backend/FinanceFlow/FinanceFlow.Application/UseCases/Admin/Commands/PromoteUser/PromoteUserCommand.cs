using MediatR;

namespace FinanceFlow.Application.UseCases.Admin.Commands.PromoteUser;

public record PromoteUserCommand(Guid TargetUserId) : IRequest;
