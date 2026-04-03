using MediatR;

namespace FinanceFlow.Application.UseCases.Auth.Commands.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest;
