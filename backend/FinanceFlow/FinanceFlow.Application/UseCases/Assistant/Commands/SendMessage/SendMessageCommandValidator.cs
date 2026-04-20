using FluentValidation;

namespace FinanceFlow.Application.UseCases.Assistant.Commands.SendMessage;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("A mensagem não pode ser vazia.")
            .MaximumLength(1000)
            .WithMessage("A mensagem não pode exceder 1000 caracteres.");
    }
}
