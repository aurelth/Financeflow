using FinanceFlow.Application.Common.Interfaces;
using FinanceFlow.Application.DTOs.QuickEntry;
using FinanceFlow.Application.Services;
using FinanceFlow.Application.Services.Ai;
using FinanceFlow.Domain.Entities;
using FinanceFlow.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinanceFlow.Application.UseCases.QuickEntry.Commands.ParseQuickEntry;

public class ParseQuickEntryCommandHandler(
    IAiQuotaService quotaService,
    IAnthropicService anthropicService,
    ICategoryRepository categoryRepository,
    ICategoryRuleRepository categoryRuleRepository,
    IOptions<AiOptions> options,
    ILogger<ParseQuickEntryCommandHandler> logger)
    : IRequestHandler<ParseQuickEntryCommand, QuickEntryResultDto>
{
    public async Task<QuickEntryResultDto> Handle(
        ParseQuickEntryCommand request,
        CancellationToken cancellationToken)
    {
        var limits = options.Value;

        // Imagem inválida é erro do usuário e barato de detectar: acontece antes de gastar a cota
        NormalizedImage? image = request.Image is { Length: > 0 }
            ? ReceiptImageNormalizer.Normalize(request.Image)
            : null;

        var lease = await quotaService.AcquireAsync(request.UserId, cancellationToken);

        try
        {
            // Só as categorias que o usuário pode usar num lançamento (sem arquivadas nem de metas)
            var categories = (await categoryRepository.GetAllByUserAsync(request.UserId, cancellationToken))
                .Where(c => c.Type is TransactionType.Expense or TransactionType.Income)
                .ToList();

            var system = QuickEntryPrompt.BuildSystem(lease.Day, categories, limits.MaxDraftsPerRequest);
            var userMessage = QuickEntryPrompt.BuildUserMessage(request.Text, image is not null);
            IReadOnlyList<AiImage> images = image is null ? [] : [new AiImage(image.Data, image.MediaType)];

            // Texto e imagem do usuário não vão para o log: só contagens
            logger.LogInformation(
                "Lançamento por IA: texto={HasText}, imagem={HasImage}.",
                !string.IsNullOrWhiteSpace(request.Text), image is not null);

            var reply = await anthropicService.SendMessageWithImagesAsync(system, userMessage, images, cancellationToken);

            var parsed = QuickEntryReplyParser.Parse(reply, lease.Day, categories, limits.MaxDraftsPerRequest);
            var drafts = await ApplyLearnedRulesAsync(request.UserId, parsed, categories, cancellationToken);

            var status = await quotaService.GetStatusAsync(request.UserId, cancellationToken);
            return new QuickEntryResultDto(drafts, ToDto(status));
        }
        catch
        {
            // Falhou antes de entregar um resultado: o usuário não paga a chamada.
            // CancellationToken.None: se o motivo foi o cancelamento, a devolução ainda precisa acontecer.
            await quotaService.ReleaseAsync(lease, CancellationToken.None);
            throw;
        }
    }

    /// <summary>
    /// O hábito do próprio usuário (regra aprendida nas importações) vale mais que o palpite da IA:
    /// se ele sempre classificou "iFood" como Alimentação, é isso que o rascunho traz.
    /// </summary>
    private async Task<IReadOnlyList<QuickEntryDraftDto>> ApplyLearnedRulesAsync(
        Guid userId,
        IReadOnlyList<ParsedEntry> parsed,
        IReadOnlyCollection<Category> categories,
        CancellationToken cancellationToken)
    {
        if (parsed.Count == 0)
            return [];

        var keys = parsed
            .Select(p => DescriptionNormalizer.ToKey(p.Description))
            .Where(k => k.Length > 0)
            .Distinct()
            .ToList();

        var rules = keys.Count == 0
            ? []
            : await categoryRuleRepository.GetByKeysAsync(userId, keys, cancellationToken);

        var byId = categories.ToDictionary(c => c.Id);
        var ruleCategory = rules
            .Where(r => byId.TryGetValue(r.CategoryId, out var c) && c.Type == r.Type)
            .ToDictionary(r => (r.Type, r.Key), r => r.CategoryId);

        return parsed
            .Select(p =>
            {
                var key = DescriptionNormalizer.ToKey(p.Description);
                var categoryId = key.Length > 0 && ruleCategory.TryGetValue((p.Type, key), out var learned)
                    ? learned
                    : p.CategoryId;

                return new QuickEntryDraftDto(
                    Type: p.Type,
                    Amount: p.Amount,
                    Date: p.Date.ToDateTime(TimeOnly.MinValue),
                    Description: p.Description,
                    CategoryId: categoryId,
                    CategoryName: categoryId is { } id ? byId[id].Name : null);
            })
            .ToList();
    }

    internal static AiQuotaDto ToDto(AiQuotaStatus status) =>
        new(status.Limit, status.Used, status.Remaining, status.ResetsAtUtc);
}
