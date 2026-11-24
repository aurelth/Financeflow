using FinanceFlow.Application.DTOs.QuickEntry;
using MediatR;

namespace FinanceFlow.Application.UseCases.QuickEntry.Queries.GetAiQuota;

public record GetAiQuotaQuery(Guid UserId) : IRequest<AiQuotaDto>;
