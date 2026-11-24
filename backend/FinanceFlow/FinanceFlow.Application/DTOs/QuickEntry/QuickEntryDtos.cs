using FinanceFlow.Domain.Entities;

namespace FinanceFlow.Application.DTOs.QuickEntry;

/// <summary>
/// Lançamento sugerido pela IA. É só um rascunho: nada é gravado até o usuário conferir e salvar no formulário.
/// </summary>
/// <param name="CategoryId">Só vem preenchida quando é uma categoria do usuário, do mesmo tipo do lançamento.</param>
public record QuickEntryDraftDto(
    TransactionType Type,
    decimal Amount,
    DateTime Date,
    string Description,
    Guid? CategoryId,
    string? CategoryName
);

/// <param name="Limit">Chamadas permitidas por dia.</param>
/// <param name="ResetsAtUtc">Quando a cota volta (meia-noite do usuário), em UTC.</param>
public record AiQuotaDto(int Limit, int Used, int Remaining, DateTime ResetsAtUtc);

public record QuickEntryResultDto(
    IReadOnlyList<QuickEntryDraftDto> Drafts,
    AiQuotaDto Quota
);
