using FinanceFlow.Application.DTOs.QuickEntry;
using FinanceFlow.Application.Services.Ai;
using MediatR;

namespace FinanceFlow.Application.UseCases.QuickEntry.Queries.GetAiQuota;

public class GetAiQuotaQueryHandler(IAiQuotaService quotaService)
    : IRequestHandler<GetAiQuotaQuery, AiQuotaDto>
{
    public async Task<AiQuotaDto> Handle(GetAiQuotaQuery request, CancellationToken cancellationToken)
    {
        var status = await quotaService.GetStatusAsync(request.UserId, cancellationToken);
        return new AiQuotaDto(status.Limit, status.Used, status.Remaining, status.ResetsAtUtc);
    }
}
