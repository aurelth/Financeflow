using FinanceFlow.Application.DTOs;
using MediatR;

namespace FinanceFlow.Application.UseCases.Dashboard.Queries.GetBalanceEvolution;

public record GetBalanceEvolutionQuery(
    Guid UserId,
    int Month,
    int Year
) : IRequest<IEnumerable<BalanceEvolutionDto>>;
