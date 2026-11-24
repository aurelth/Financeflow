using FinanceFlow.Application.Services.Ai;
using FluentValidation;
using Microsoft.Extensions.Options;

namespace FinanceFlow.Application.UseCases.QuickEntry.Commands.ParseQuickEntry;

public class ParseQuickEntryCommandValidator : AbstractValidator<ParseQuickEntryCommand>
{
    public ParseQuickEntryCommandValidator(IOptions<AiOptions> options)
    {
        var limits = options.Value;

        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.Text) || x.Image is { Length: > 0 })
            .WithName("Entrada")
            .WithMessage("Descreva o gasto em texto ou envie a foto do comprovante.");

        RuleFor(x => x.Text)
            .MaximumLength(limits.MaxTextLength)
            .WithMessage($"O texto deve ter no máximo {limits.MaxTextLength} caracteres.");

        RuleFor(x => x.Image)
            .Must(image => image is null || image.Length <= limits.MaxImageBytes)
            .WithMessage($"A imagem deve ter no máximo {limits.MaxImageBytes / (1024 * 1024)} MB.");
    }
}
