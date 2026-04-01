using FluentValidation;

namespace FinanceFlow.Application.UseCases.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId é obrigatório.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Tipo é obrigatório.")
            .MaximumLength(50).WithMessage("Tipo não pode exceder 50 caracteres.");

        RuleFor(x => x.Message)
            .NotEmpty().WithMessage("Mensagem é obrigatória.")
            .MaximumLength(500).WithMessage("Mensagem não pode exceder 500 caracteres.");
    }
}
