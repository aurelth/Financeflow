using FinanceFlow.Application.DTOs.QuickEntry;
using MediatR;

namespace FinanceFlow.Application.UseCases.QuickEntry.Commands.ParseQuickEntry;

/// <param name="Text">Texto livre ("almoço 45 no iFood ontem"). Opcional se houver imagem.</param>
/// <param name="Image">Bytes da foto do comprovante. Opcional se houver texto.</param>
public record ParseQuickEntryCommand(Guid UserId, string? Text, byte[]? Image)
    : IRequest<QuickEntryResultDto>;
