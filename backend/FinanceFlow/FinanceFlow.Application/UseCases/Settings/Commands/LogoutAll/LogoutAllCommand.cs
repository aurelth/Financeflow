using MediatR;

namespace FinanceFlow.Application.UseCases.Settings.Commands.LogoutAll;

public record LogoutAllCommand(Guid UserId) : IRequest;
